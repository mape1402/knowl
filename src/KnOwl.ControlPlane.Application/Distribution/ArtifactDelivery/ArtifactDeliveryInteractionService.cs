using KnOwl.Contracts.Distribution;
using System.Text;
using System.Text.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;

namespace KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;

/// <inheritdoc />
internal sealed class ArtifactDeliveryInteractionService(
    HttpClient httpClient,
    IContractReleaseTargetRepository targets,
    IContractReleaseRepository releases,
    IRuntimeAccessTokenProvider accessTokenProvider) : IArtifactDeliveryInteractionService
{
    private const string DefaultRuntimeDeployPath = "runtime/artifacts/deploy";

    /// <inheritdoc />
    public async Task<RuntimeArtifactDeliveryResult> Push(Guid releaseTargetId, string initiatedBy = "distribution", CancellationToken cancellationToken = default)
    {
        var startedAtUtc = DateTime.UtcNow;
        var target = await targets.GetById(releaseTargetId, includeArtifact: true, cancellationToken)
            ?? throw new KeyNotFoundException($"Release target '{releaseTargetId}' was not found.");
        var runtimeNode = target.RuntimeNode
            ?? throw new InvalidOperationException($"Release target '{releaseTargetId}' does not include its runtime node snapshot.");

        try
        {
            EnsureRuntimeNodeCanDistribute(runtimeNode);

            if (target.Status == ContractReleaseTargetStatus.Activated &&
                target.ActivationStatus == ContractReleaseActivationStatus.Activated &&
                !string.IsNullOrWhiteSpace(target.RuntimeVersionApplied))
            {
                return CreateResult(target, true, "Runtime artifact was already delivered.", target.RuntimeVersionApplied);
            }

            if (runtimeNode.DistributionMode == DistributionMode.Pull)
            {
                target.Status = ContractReleaseTargetStatus.AvailableForPull;
                target.AvailableAtUtc ??= DateTime.UtcNow;
                await targets.Update(target, cancellationToken);
                await AddAttempt(target.Id, "Pull", initiatedBy, startedAtUtc, true, string.Empty, string.Empty, cancellationToken);
                await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
                return CreateResult(target, true, "Runtime node is configured for pull.");
            }

            if (runtimeNode.OutboundCredentialStatus != ConnectionCredentialStatus.Active)
            {
                throw new InvalidOperationException($"Runtime node '{runtimeNode.Name}' outbound credential is not active.");
            }

            if (string.IsNullOrWhiteSpace(runtimeNode.EndpointBaseUri))
            {
                throw new InvalidOperationException("Runtime node endpoint is not configured for push.");
            }

            target.Status = ContractReleaseTargetStatus.InProgress;
            await targets.Update(target, cancellationToken);

            var package = BuildPackage(target);
            using var request = await CreateAuthenticatedDeployRequest(runtimeNode, package, cancellationToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var bodyText = await response.Content.ReadAsStringAsync(cancellationToken);
            var body = TryReadDeploymentResponse(bodyText);

            if (!response.IsSuccessStatusCode || body?.Accepted != true)
            {
                throw new InvalidOperationException(body?.Message ?? ExtractRuntimeError(bodyText) ?? $"Runtime push returned HTTP {(int)response.StatusCode}.");
            }

            target.DeliveredAtUtc = DateTime.UtcNow;
            target.AcknowledgedAtUtc = target.DeliveredAtUtc;
            target.RuntimeVersionApplied = string.IsNullOrWhiteSpace(body.RuntimeArtifactId) ? string.Empty : body.RuntimeArtifactId.Trim();
            target.FailureReason = string.Empty;
            await AddAttempt(target.Id, "Push", initiatedBy, startedAtUtc, true, target.RuntimeVersionApplied, string.Empty, cancellationToken);

            if (IsRuntimeReady(body.Status))
            {
                target.Status = ContractReleaseTargetStatus.Activated;
                target.ActivationStatus = ContractReleaseActivationStatus.Activated;
                target.ActivatedAtUtc = target.DeliveredAtUtc;
                await targets.Update(target, cancellationToken);
                await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
                return CreateResult(target, true, "Runtime artifact pushed and activated.", target.RuntimeVersionApplied);
            }

            target.Status = ContractReleaseTargetStatus.Delivered;
            target.ActivationStatus = ContractReleaseActivationStatus.Activating;
            await targets.Update(target, cancellationToken);
            await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
            return CreateResult(target, true, "Runtime artifact pushed and accepted.", target.RuntimeVersionApplied);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            var fallbackToPull = runtimeNode.DistributionMode == DistributionMode.Hybrid;
            target.Status = runtimeNode.DistributionMode == DistributionMode.Hybrid
                ? ContractReleaseTargetStatus.AvailableForPull
                : ContractReleaseTargetStatus.Failed;
            target.ActivationStatus = ContractReleaseActivationStatus.ActivationFailed;
            target.AvailableAtUtc = runtimeNode.DistributionMode == DistributionMode.Hybrid ? DateTime.UtcNow : target.AvailableAtUtc;
            target.FailedAtUtc = DateTime.UtcNow;
            target.FailureReason = ex.Message;
            await targets.Update(target, cancellationToken);
            await AddAttempt(target.Id, "Push", initiatedBy, startedAtUtc, false, string.Empty, ex.Message, cancellationToken);
            if (fallbackToPull)
            {
                await AddAttempt(target.Id, "Pull", initiatedBy, DateTime.UtcNow, true, string.Empty, "Hybrid fallback made artifact available for runtime pull.", cancellationToken);
            }

            await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
            return CreateResult(target, false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPendingForPull(Guid runtimeNodeId, CancellationToken cancellationToken = default)
    {
        var pendingTargets = await targets.GetPendingForRuntimeNode(runtimeNodeId, cancellationToken);
        List<RuntimeArtifactDeliveryPackage> packages = [];
        foreach (var target in pendingTargets)
        {
            var runtimeNode = target.RuntimeNode
                ?? throw new InvalidOperationException($"Release target '{target.Id}' does not include its runtime node snapshot.");
            EnsureRuntimeNodeCanPull(runtimeNode);
            if (!IsTargetAvailableForPull(runtimeNode, target))
            {
                continue;
            }

            if (target.Status is ContractReleaseTargetStatus.Pending or ContractReleaseTargetStatus.PushScheduled)
            {
                target.Status = ContractReleaseTargetStatus.AvailableForPull;
                target.AvailableAtUtc ??= DateTime.UtcNow;
                await targets.Update(target, cancellationToken);
                await AddAttempt(target.Id, "Pull", "runtime", DateTime.UtcNow, true, string.Empty, string.Empty, cancellationToken);
                await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
            }

            packages.Add(BuildPackage(target));
        }

        return packages;
    }

    /// <inheritdoc />
    public async Task<RuntimeArtifactDeliveryPackage> GetForPull(
        Guid runtimeNodeId,
        Guid releaseTargetId,
        CancellationToken cancellationToken = default)
    {
        var target = await targets.GetById(releaseTargetId, includeArtifact: true, cancellationToken)
            ?? throw new KeyNotFoundException($"Release target '{releaseTargetId}' was not found.");
        if (target.RuntimeNodeId != runtimeNodeId)
        {
            throw new InvalidOperationException($"Release target '{releaseTargetId}' does not belong to runtime node '{runtimeNodeId}'.");
        }

        var runtimeNode = target.RuntimeNode
            ?? throw new InvalidOperationException($"Release target '{releaseTargetId}' does not include its runtime node snapshot.");
        EnsureRuntimeNodeCanPull(runtimeNode);
        if (!IsTargetAvailableForPull(runtimeNode, target))
        {
            throw new InvalidOperationException($"Release target '{releaseTargetId}' is not available for pull.");
        }

        if (target.Status is ContractReleaseTargetStatus.Pending or ContractReleaseTargetStatus.PushScheduled)
        {
            target.Status = ContractReleaseTargetStatus.AvailableForPull;
            target.AvailableAtUtc ??= DateTime.UtcNow;
            await targets.Update(target, cancellationToken);
            await AddAttempt(target.Id, "Pull", "runtime", DateTime.UtcNow, true, string.Empty, string.Empty, cancellationToken);
            await RefreshReleaseStatus(target.ReleaseId, cancellationToken);
        }

        return BuildPackage(target);
    }

    /// <inheritdoc />
    public async Task<RuntimeArtifactDeliveryResult> AcknowledgePull(
        Guid runtimeNodeId,
        Guid releaseTargetId,
        string runtimeArtifactId,
        string runtimeArtifactStatus = "Ready",
        CancellationToken cancellationToken = default)
    {
        var target = await targets.GetById(releaseTargetId, includeArtifact: true, cancellationToken)
            ?? throw new KeyNotFoundException($"Release target '{releaseTargetId}' was not found.");

        if (target.RuntimeNodeId != runtimeNodeId)
        {
            throw new InvalidOperationException($"Release target '{releaseTargetId}' does not belong to runtime node '{runtimeNodeId}'.");
        }

        var ready = IsRuntimeReady(runtimeArtifactStatus);
        target.Status = ready ? ContractReleaseTargetStatus.Activated : ContractReleaseTargetStatus.Acknowledged;
        target.ActivationStatus = ready ? ContractReleaseActivationStatus.Activated : ContractReleaseActivationStatus.Activating;
        target.DeliveredAtUtc ??= DateTime.UtcNow;
        target.AcknowledgedAtUtc = DateTime.UtcNow;
        target.ActivatedAtUtc = ready ? target.AcknowledgedAtUtc : null;
        target.RuntimeVersionApplied = string.IsNullOrWhiteSpace(runtimeArtifactId) ? string.Empty : runtimeArtifactId.Trim();
        target.FailureReason = string.Empty;
        await targets.Update(target, cancellationToken);
        await AddAttempt(target.Id, "Ack", "runtime", DateTime.UtcNow, true, target.RuntimeVersionApplied, string.Empty, cancellationToken);
        await RefreshReleaseStatus(target.ReleaseId, cancellationToken);

        return ready
            ? CreateResult(target, true, "Runtime pull acknowledged and activated.", target.RuntimeVersionApplied)
            : CreateResult(target, true, "Runtime pull acknowledged and accepted.", target.RuntimeVersionApplied);
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedDeployRequest(RuntimeNode runtimeNode, RuntimeArtifactDeliveryPackage package, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(package, JsonOptions());
        var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint(runtimeNode))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        await accessTokenProvider.AttachToken(request, runtimeNode, [ArtifactDeliveryScope.ArtifactPush], cancellationToken);
        return request;
    }

    private static Uri BuildEndpoint(RuntimeNode runtimeNode)
    {
        var baseUri = runtimeNode.EndpointBaseUri.TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(runtimeNode.EndpointApiPath)
            ? DefaultRuntimeDeployPath
            : runtimeNode.EndpointApiPath.Trim('/');
        return new Uri($"{baseUri}/{path}", UriKind.Absolute);
    }

    private static RuntimeArtifactDeliveryPackage BuildPackage(ContractReleaseTarget target)
    {
        var artifact = target.Artifact
            ?? throw new InvalidOperationException($"Release target '{target.Id}' does not include its artifact snapshot.");

        return new RuntimeArtifactDeliveryPackage
        {
            ReleaseTargetId = target.Id,
            ReleaseId = target.ReleaseId,
            RuntimeNodeId = target.RuntimeNodeId,
            ArtifactId = artifact.Id,
            ArtifactType = artifact.ArtifactType,
            SchemaVersion = "1.0.0",
            EnvironmentKey = target.RuntimeNode?.Environment?.Code ?? target.RuntimeNode?.EnvironmentName ?? string.Empty,
            DefinitionId = artifact.DefinitionId,
            VersionId = artifact.VersionId,
            Topic = artifact.Topic,
            VersionNumber = artifact.VersionNumber,
            Name = artifact.Name,
            Description = artifact.Description,
            PayloadSchemaJson = artifact.PayloadSchemaJson,
            ContentHash = artifact.ContentHash,
            CorrelationId = string.IsNullOrWhiteSpace(target.CorrelationId) ? target.ReleaseId.ToString("N") : target.CorrelationId,
            PromotedBy = "distribution",
            PromotedAtUtc = DateTime.UtcNow
        };
    }

    private static RuntimeArtifactDeliveryResult CreateResult(ContractReleaseTarget target, bool succeeded, string message, string runtimeArtifactId = "")
    {
        return new RuntimeArtifactDeliveryResult
        {
            Succeeded = succeeded,
            ReleaseTargetId = target.Id,
            RuntimeNodeId = target.RuntimeNodeId,
            ArtifactId = target.ArtifactId,
            Status = target.Status.ToString(),
            Message = message,
            RuntimeArtifactId = runtimeArtifactId,
            ExternalReference = runtimeArtifactId
        };
    }

    private async Task RefreshReleaseStatus(Guid releaseId, CancellationToken cancellationToken)
    {
        var releaseTargets = await targets.GetByRelease(releaseId, cancellationToken);
        if (releaseTargets.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (releaseTargets.All(x => x.Status == ContractReleaseTargetStatus.Activated))
        {
            await releases.UpdateStatus(releaseId, ContractReleaseStatus.Completed, now, cancellationToken);
            return;
        }

        if (releaseTargets.Any(x => x.Status == ContractReleaseTargetStatus.Failed))
        {
            await releases.UpdateStatus(releaseId, ContractReleaseStatus.Failed, now, cancellationToken);
            return;
        }

        await releases.UpdateStatus(releaseId, ContractReleaseStatus.InProgress, now, cancellationToken);
    }

    private async Task AddAttempt(
        Guid releaseTargetId,
        string action,
        string initiatedBy,
        DateTime startedAtUtc,
        bool succeeded,
        string externalReference,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        await targets.AddAttempt(new ContractReleaseAttempt
        {
            ReleaseTargetId = releaseTargetId,
            Action = action,
            InitiatedBy = string.IsNullOrWhiteSpace(initiatedBy) ? "distribution" : initiatedBy.Trim(),
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = DateTime.UtcNow,
            Succeeded = succeeded,
            ErrorCode = succeeded ? string.Empty : "ArtifactDeliveryFailed",
            ErrorMessage = errorMessage,
            ExternalReference = externalReference
        }, cancellationToken);
    }

    private static RuntimeArtifactDeploymentResult? TryReadDeploymentResponse(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText) || !bodyText.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<RuntimeArtifactDeploymentResult>(bodyText, JsonOptions());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractRuntimeError(string bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
        {
            return null;
        }

        var firstLine = bodyText
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        return string.IsNullOrWhiteSpace(firstLine)
            ? null
            : firstLine.Length > 500 ? firstLine[..500] : firstLine;
    }

    private static void EnsureRuntimeNodeCanDistribute(RuntimeNode node)
    {
        if (node.IsDeleted || !node.IsEnabled || node.Status != RuntimeNodeStatus.Active)
        {
            throw new InvalidOperationException($"Runtime node '{node.Name}' is not active and enabled.");
        }
    }

    private static void EnsureRuntimeNodeCanPull(RuntimeNode node)
    {
        EnsureRuntimeNodeCanDistribute(node);

        if (node.DistributionMode is not (DistributionMode.Pull or DistributionMode.Hybrid))
        {
            throw new InvalidOperationException($"Runtime node '{node.Name}' is not configured for pull.");
        }
    }

    private static bool IsTargetAvailableForPull(RuntimeNode node, ContractReleaseTarget target)
    {
        return target.Status is ContractReleaseTargetStatus.AvailableForPull
            or ContractReleaseTargetStatus.Pending
            || (node.DistributionMode == DistributionMode.Hybrid
                && target.Status == ContractReleaseTargetStatus.PushScheduled);
    }

    private static bool IsRuntimeReady(string status)
        => string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Activated", StringComparison.OrdinalIgnoreCase);

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}


using KnOwl.Contracts.Distribution;
using System.Net.Http.Json;
using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;

namespace KnOwl.Runtime.Application.ArtifactDelivery;

/// <inheritdoc />
public sealed class ControlPlaneArtifactPullService(
    HttpClient httpClient,
    IRuntimeDesignNodeRepository designNodes,
    IControlPlaneAccessTokenProvider accessTokenProvider,
    IRuntimeContractDeploymentService deploymentService) : IControlPlaneArtifactPullService
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ControlPlaneDistributionSource>> GetSources(CancellationToken cancellationToken = default)
    {
        var nodes = await designNodes.GetAll(cancellationToken);
        return nodes
            .Where(IsPullSourceReady)
            .Select(ToSource)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>> GetPending(string sourceKey, CancellationToken cancellationToken = default)
    {
        var source = await GetSource(sourceKey, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildPendingUri(source));
        await accessTokenProvider.AttachToken(request, source, [ArtifactDeliveryScope.ReleaseRead], cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var packages = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<RuntimeArtifactDeliveryPackage>>(cancellationToken);
        return packages ?? [];
    }

    /// <inheritdoc />
    public async Task<RuntimeArtifactDeploymentResult> Apply(string sourceKey, Guid releaseTargetId, CancellationToken cancellationToken = default)
    {
        var pending = await GetPending(sourceKey, cancellationToken);
        var package = pending.FirstOrDefault(x => x.ReleaseTargetId == releaseTargetId)
            ?? throw new KeyNotFoundException($"Release target '{releaseTargetId}' is not pending for source '{sourceKey}'.");

        return await ApplyPackage(sourceKey, package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeArtifactDeploymentResult> ApplyPackage(
        string sourceKey,
        RuntimeArtifactDeliveryPackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        var source = await GetSource(sourceKey, cancellationToken);
        var deployment = await deploymentService.DeployArtifact(package, source.Key, cancellationToken);
        if (deployment.Accepted)
        {
            using var ack = new HttpRequestMessage(HttpMethod.Post, BuildAckUri(source, package.ReleaseTargetId))
            {
                Content = JsonContent.Create(new RuntimeArtifactPullAckRequest
                {
                    RuntimeArtifactId = deployment.RuntimeArtifactId,
                    RuntimeArtifactStatus = deployment.Status
                })
            };
            await accessTokenProvider.AttachToken(ack, source, [ArtifactDeliveryScope.ArtifactAcknowledge], cancellationToken);
            using var response = await httpClient.SendAsync(ack, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        return deployment;
    }

    private async Task<ControlPlaneDistributionSource> GetSource(string sourceKey, CancellationToken cancellationToken)
    {
        var node = await designNodes.GetByKey(sourceKey, cancellationToken)
            ?? throw new KeyNotFoundException($"Control Plane source '{sourceKey}' was not found.");

        if (!node.IsEnabled || node.Status != RuntimeDesignNodeStatus.Enabled)
        {
            throw new InvalidOperationException($"Control Plane source '{sourceKey}' is not enabled.");
        }

        if (node.DistributionMode is not (DistributionMode.Pull or DistributionMode.Hybrid))
        {
            throw new InvalidOperationException($"Control Plane source '{sourceKey}' is not configured for pull.");
        }

        if (node.OutboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            throw new InvalidOperationException($"Control Plane source '{sourceKey}' outbound credential is not active.");
        }

        if (string.IsNullOrWhiteSpace(node.RemoteRuntimeNodeId))
        {
            throw new InvalidOperationException($"Control Plane source '{sourceKey}' does not have a remote runtime node id.");
        }

        return ToSource(node);
    }

    private static Uri BuildPendingUri(ControlPlaneDistributionSource source)
        => new($"{source.EndpointBaseUri.TrimEnd('/')}/distribution/artifacts/runtime-nodes/{source.RemoteRuntimeNodeId}/pending", UriKind.Absolute);

    private static Uri BuildAckUri(ControlPlaneDistributionSource source, Guid releaseTargetId)
        => new($"{source.EndpointBaseUri.TrimEnd('/')}/distribution/artifacts/runtime-nodes/{source.RemoteRuntimeNodeId}/targets/{releaseTargetId}/ack", UriKind.Absolute);

    private static ControlPlaneDistributionSource ToSource(RuntimeDesignNode node)
        => new()
        {
            Key = node.Key,
            Name = node.Name,
            EndpointBaseUri = node.EndpointBaseUri,
            RemoteRuntimeNodeId = node.RemoteRuntimeNodeId,
            ClientId = node.OutboundClientId,
            ProtectedSecret = node.ProtectedOutboundSecret,
            KeyId = node.OutboundKeyId,
            RequestedScopes = node.OutboundRequestedScopes,
            TokenRefreshSkewSeconds = node.TokenRefreshSkewSeconds,
            IsEnabled = node.IsEnabled
        };

    private static bool IsPullSourceReady(RuntimeDesignNode node)
    {
        return node.IsEnabled
            && node.Status == RuntimeDesignNodeStatus.Enabled
            && node.DistributionMode is DistributionMode.Pull or DistributionMode.Hybrid
            && node.OutboundCredentialStatus == ConnectionCredentialStatus.Active
            && !string.IsNullOrWhiteSpace(node.RemoteRuntimeNodeId);
    }
}


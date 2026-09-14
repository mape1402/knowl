using KnOwl.Contracts.Distribution;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Default Control Plane-side Runtime node credential exchange service.
/// </summary>
public sealed class RuntimeNodeConnectionInteractionService(
    HttpClient httpClient,
    IRuntimeNodeRepository runtimeNodes,
    IConnectionSecretGenerator secretGenerator,
    IConnectionSecretHasher secretHasher,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionCredentialPackageSerializer packageSerializer,
    IControlPlaneRuntimeNodeSecretProtector secretProtector,
    IRuntimeAccessTokenProvider accessTokenProvider) : IRuntimeNodeConnectionInteractionService
{
    /// <inheritdoc />
    public async Task<RuntimeNodeCredentialPackageModel> GenerateCredentialPackage(
        Guid runtimeNodeId,
        string issuerBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var node = await GetNode(runtimeNodeId, cancellationToken);
        var material = secretGenerator.GenerateCredential(node.Code);
        var now = DateTime.UtcNow;
        var scopes = scopeFormatter.FormatMany(GetInboundScopes(node.DistributionMode));

        node.InboundClientId = material.ClientId;
        node.InboundKeyId = material.KeyId;
        node.InboundSecretHash = secretHasher.HashSecret(material.ClientSecret);
        node.InboundAllowedScopes = scopes;
        node.InboundCredentialStatus = ConnectionCredentialStatus.Active;
        node.InboundCredentialCreatedAtUtc ??= now;
        node.InboundCredentialRotatedAtUtc = now;
        node.InboundCredentialRevokedAtUtc = null;
        await runtimeNodes.Update(node, cancellationToken);

        var envelope = packageSerializer.CreateEnvelope(new ConnectionCredentialPackage
        {
            Issuer = "control-plane",
            Target = "runtime",
            Mode = node.DistributionMode.ToString(),
            BaseUrl = issuerBaseUrl?.Trim().TrimEnd('/') ?? string.Empty,
            IssuerNodeId = "control-plane",
            IssuerNodeCode = "control-plane",
            TargetNodeId = node.Id.ToString("N"),
            TargetNodeCode = node.Code,
            ClientId = material.ClientId,
            ClientSecret = material.ClientSecret,
            KeyId = material.KeyId,
            Scopes = scopes,
            GeneratedAtUtc = now
        });

        return new RuntimeNodeCredentialPackageModel { Json = envelope.Json, Base64 = envelope.Base64 };
    }

    /// <inheritdoc />
    public async Task ImportCredentialPackage(ImportRuntimeNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var node = await GetNode(input.RuntimeNodeId, cancellationToken);
        var package = packageSerializer.Parse(input.Package);
        if (!string.Equals(package.Issuer, "runtime", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(package.Target, "control-plane", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Credential package must be issued by Runtime for Control Plane.");
        }

        node.EndpointBaseUri = string.IsNullOrWhiteSpace(package.BaseUrl) ? node.EndpointBaseUri : package.BaseUrl.Trim().TrimEnd('/');
        node.OutboundClientId = package.ClientId;
        node.OutboundKeyId = package.KeyId;
        node.ProtectedOutboundSecret = secretProtector.Protect(package.ClientSecret);
        node.OutboundRequestedScopes = package.Scopes;
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Active;
        node.OutboundCredentialImportedAtUtc = DateTime.UtcNow;
        await runtimeNodes.Update(node, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeNodeConnectionValidationModel> ValidateConnection(Guid runtimeNodeId, CancellationToken cancellationToken = default)
    {
        var node = await GetNode(runtimeNodeId, cancellationToken);
        try
        {
            EnsureCanCheckConnection(node);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"{node.EndpointBaseUri.TrimEnd('/')}/runtime/distribution/connect/validate", UriKind.Absolute));
            await accessTokenProvider.AttachToken(
                request,
                node,
                [ArtifactDeliveryScope.ConnectionValidate],
                cancellationToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
                    ? $"Runtime returned HTTP {(int)response.StatusCode}."
                    : body);
            }

            return new RuntimeNodeConnectionValidationModel { Succeeded = true, Message = "Connection validated." };
        }
        catch (Exception ex)
        {
            return new RuntimeNodeConnectionValidationModel { Succeeded = false, Message = ex.Message };
        }
    }

    private async Task<RuntimeNode> GetNode(Guid runtimeNodeId, CancellationToken cancellationToken)
    {
        return await runtimeNodes.GetById(runtimeNodeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Runtime node '{runtimeNodeId}' was not found.");
    }

    private static IReadOnlyCollection<ArtifactDeliveryScope> GetInboundScopes(DistributionMode mode)
        => mode is DistributionMode.Pull or DistributionMode.Hybrid
            ? [ArtifactDeliveryScope.ReleaseRead, ArtifactDeliveryScope.ArtifactRead, ArtifactDeliveryScope.ArtifactAcknowledge, ArtifactDeliveryScope.ConnectionValidate]
            : [ArtifactDeliveryScope.ConnectionValidate];

    private static void EnsureCanCheckConnection(RuntimeNode node)
    {
        if (node.DistributionMode is not (DistributionMode.Push or DistributionMode.Hybrid))
        {
            throw new InvalidOperationException("Connection check only applies when Control Plane can call Runtime.");
        }

        if (node.IsDeleted || !node.IsEnabled || node.Status != RuntimeNodeStatus.Active)
        {
            throw new InvalidOperationException("Runtime node must be active and enabled before checking the connection.");
        }

        if (node.OutboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            throw new InvalidOperationException("Runtime credentials are required before checking the connection.");
        }

        if (string.IsNullOrWhiteSpace(node.EndpointBaseUri))
        {
            throw new InvalidOperationException("Runtime endpoint is required before checking the connection.");
        }
    }
}


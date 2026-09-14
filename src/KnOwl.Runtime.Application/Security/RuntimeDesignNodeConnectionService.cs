using KnOwl.Contracts.Distribution;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Default Runtime-side Control Plane credential exchange service.
/// </summary>
public sealed class RuntimeDesignNodeConnectionService(
    HttpClient httpClient,
    IRuntimeDesignNodeRepository designNodes,
    IConnectionSecretGenerator secretGenerator,
    IConnectionSecretHasher secretHasher,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionCredentialPackageSerializer packageSerializer,
    IRuntimeDesignNodeSecretProtector secretProtector,
    IControlPlaneAccessTokenProvider accessTokenProvider) : IRuntimeDesignNodeConnectionService
{
    /// <inheritdoc />
    public async Task<RuntimeDesignNode> UpsertDesignNode(
        Guid? id,
        string key,
        string name,
        string endpointBaseUri,
        string remoteRuntimeNodeId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Design node key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Design node name is required.", nameof(name));
        }

        RuntimeDesignNode? node = null;
        if (id is not null && id.Value != Guid.Empty)
        {
            node = await designNodes.GetById(id.Value, cancellationToken);
        }

        node ??= new RuntimeDesignNode { Id = id.GetValueOrDefault(Guid.NewGuid()), CreatedAtUtc = DateTime.UtcNow };
        node.Key = key.Trim();
        node.Name = name.Trim();
        node.EndpointBaseUri = endpointBaseUri?.Trim().TrimEnd('/') ?? string.Empty;
        node.RemoteRuntimeNodeId = remoteRuntimeNodeId?.Trim() ?? string.Empty;
        node.IsEnabled = isEnabled;
        node.Status = isEnabled ? RuntimeDesignNodeStatus.Enabled : RuntimeDesignNodeStatus.Pending;
        node.UpdatedAtUtc = DateTime.UtcNow;

        await designNodes.Upsert(node, cancellationToken);
        return node;
    }

    /// <inheritdoc />
    public async Task<RuntimeDesignNodeCredentialPackageModel> GenerateCredentialPackage(
        Guid designNodeId,
        string issuerBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var node = await GetNode(designNodeId, cancellationToken);
        var material = secretGenerator.GenerateCredential(node.Key);
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
        await designNodes.Upsert(node, cancellationToken);

        var envelope = packageSerializer.CreateEnvelope(new ConnectionCredentialPackage
        {
            Issuer = "runtime",
            Target = "control-plane",
            Mode = node.DistributionMode.ToString(),
            BaseUrl = issuerBaseUrl?.Trim().TrimEnd('/') ?? string.Empty,
            IssuerNodeId = node.Id.ToString("N"),
            IssuerNodeCode = node.Key,
            TargetNodeId = "control-plane",
            TargetNodeCode = "control-plane",
            ClientId = material.ClientId,
            ClientSecret = material.ClientSecret,
            KeyId = material.KeyId,
            Scopes = scopes,
            GeneratedAtUtc = now
        });

        return new RuntimeDesignNodeCredentialPackageModel { Json = envelope.Json, Base64 = envelope.Base64 };
    }

    /// <inheritdoc />
    public async Task ImportCredentialPackage(ImportRuntimeDesignNodeCredentialPackageInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var node = await GetNode(input.DesignNodeId, cancellationToken);
        var package = packageSerializer.Parse(input.Package);
        if (!string.Equals(package.Issuer, "control-plane", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(package.Target, "runtime", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Credential package must be issued by Control Plane for Runtime.");
        }

        node.EndpointBaseUri = string.IsNullOrWhiteSpace(package.BaseUrl) ? node.EndpointBaseUri : package.BaseUrl.Trim().TrimEnd('/');
        node.RemoteRuntimeNodeId = string.IsNullOrWhiteSpace(package.TargetNodeId) ? node.RemoteRuntimeNodeId : package.TargetNodeId.Trim();
        node.OutboundClientId = package.ClientId;
        node.OutboundKeyId = package.KeyId;
        node.ProtectedOutboundSecret = secretProtector.Protect(package.ClientSecret);
        node.OutboundRequestedScopes = package.Scopes;
        node.OutboundCredentialStatus = ConnectionCredentialStatus.Active;
        node.OutboundCredentialImportedAtUtc = DateTime.UtcNow;
        node.Status = RuntimeDesignNodeStatus.Enabled;
        node.IsEnabled = true;
        await designNodes.Upsert(node, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeDesignNodeConnectionValidationModel> ValidateConnection(Guid designNodeId, CancellationToken cancellationToken = default)
    {
        var node = await GetNode(designNodeId, cancellationToken);
        try
        {
            EnsureCanCheckConnection(node);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"{node.EndpointBaseUri.TrimEnd('/')}/distribution/runtime-nodes/{node.RemoteRuntimeNodeId}/connect/validate", UriKind.Absolute));
            await accessTokenProvider.AttachToken(
                request,
                ToSource(node),
                [ArtifactDeliveryScope.ConnectionValidate],
                cancellationToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
                    ? $"Control Plane returned HTTP {(int)response.StatusCode}."
                    : body);
            }

            return new RuntimeDesignNodeConnectionValidationModel { Succeeded = true, Message = "Connection validated." };
        }
        catch (Exception ex)
        {
            return new RuntimeDesignNodeConnectionValidationModel { Succeeded = false, Message = ex.Message };
        }
    }

    private async Task<RuntimeDesignNode> GetNode(Guid designNodeId, CancellationToken cancellationToken)
    {
        return await designNodes.GetById(designNodeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Runtime design node '{designNodeId}' was not found.");
    }

    private static IReadOnlyCollection<ArtifactDeliveryScope> GetInboundScopes(DistributionMode mode)
        => mode is DistributionMode.Push or DistributionMode.Hybrid
            ? [ArtifactDeliveryScope.ArtifactPush, ArtifactDeliveryScope.ConnectionValidate]
            : [ArtifactDeliveryScope.ConnectionValidate];

    private static void EnsureCanCheckConnection(RuntimeDesignNode node)
    {
        if (node.DistributionMode is not (DistributionMode.Pull or DistributionMode.Hybrid))
        {
            throw new InvalidOperationException("Connection check only applies when Runtime can call Control Plane.");
        }

        if (node.Status != RuntimeDesignNodeStatus.Enabled || !node.IsEnabled)
        {
            throw new InvalidOperationException("Control Plane node must be enabled before checking the connection.");
        }

        if (node.OutboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            throw new InvalidOperationException("Control Plane credentials are required before checking the connection.");
        }

        if (string.IsNullOrWhiteSpace(node.EndpointBaseUri))
        {
            throw new InvalidOperationException("Control Plane endpoint is required before checking the connection.");
        }

        if (string.IsNullOrWhiteSpace(node.RemoteRuntimeNodeId))
        {
            throw new InvalidOperationException("Remote runtime node id is required before checking the connection.");
        }
    }

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
}


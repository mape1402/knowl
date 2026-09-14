using System.Text.Json;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Default Control Plane token issuer for Runtime initiated artifact distribution calls.
/// </summary>
public sealed class ControlPlaneConnectionTokenIssuer(
    IRuntimeNodeRepository runtimeNodes,
    IConnectionSecretHasher secretHasher,
    IConnectionSecretGenerator secretGenerator,
    ITokenHashService tokenHashService,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionTokenCacheKeyBuilder cacheKeyBuilder,
    IDistributedCache cache) : IControlPlaneConnectionTokenIssuer
{
    /// <inheritdoc />
    public async Task<ConnectionTokenResponse> Issue(ConnectionTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.GrantType, "client_credentials", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only client_credentials grant type is supported.");
        }

        var node = await runtimeNodes.GetByInboundClientId(request.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("Client credential is not registered.");
        var now = DateTime.UtcNow;

        try
        {
            EnsureNodeCanIssueToken(node);
            EnsureRequestedScopesAreAllowed(request.Scope, node.InboundAllowedScopes);

            if (!secretHasher.VerifySecret(request.ClientSecret, node.InboundSecretHash))
            {
                throw new InvalidOperationException("Client credential secret is invalid.");
            }

            var token = secretGenerator.GenerateToken();
            var expiresIn = Math.Max(60, node.AccessTokenTtlSeconds);
            var expiresAtUtc = now.AddSeconds(expiresIn);
            var entry = new ConnectionTokenCacheEntry
            {
                NodeId = node.Id.ToString("N"),
                NodeKey = node.Code,
                ClientId = node.InboundClientId,
                KeyId = node.InboundKeyId,
                Scopes = request.Scope,
                ExpiresAtUtc = expiresAtUtc
            };

            var tokenHash = tokenHashService.HashToken(token);
            await cache.SetStringAsync(
                cacheKeyBuilder.BuildIssuedTokenKey("control-plane", tokenHash),
                JsonSerializer.Serialize(entry, JsonOptions()),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiresIn) },
                cancellationToken);

            node.InboundLastTokenIssuedAtUtc = now;
            node.InboundLastFailureReason = string.Empty;
            await runtimeNodes.Update(node, cancellationToken);

            return new ConnectionTokenResponse
            {
                AccessToken = token,
                ExpiresIn = expiresIn,
                ExpiresAtUtc = expiresAtUtc,
                Scope = request.Scope,
                KeyId = node.InboundKeyId
            };
        }
        catch (Exception ex)
        {
            node.InboundLastTokenFailedAtUtc = now;
            node.InboundLastFailureReason = ex.Message;
            await runtimeNodes.Update(node, cancellationToken);
            throw;
        }
    }

    private static void EnsureNodeCanIssueToken(RuntimeNode node)
    {
        if (node.IsDeleted || !node.IsEnabled || node.Status != RuntimeNodeStatus.Active)
        {
            throw new InvalidOperationException("Runtime node connection is disabled.");
        }

        if (node.InboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            throw new InvalidOperationException("Inbound credential is not active.");
        }
    }

    private void EnsureRequestedScopesAreAllowed(string requestedValue, string allowedValue)
    {
        var requested = scopeFormatter.ParseMany(requestedValue);
        var allowed = scopeFormatter.ParseMany(allowedValue);

        if (requested.Count == 0)
        {
            throw new InvalidOperationException("At least one scope is required.");
        }

        if (requested.Any(scope => !allowed.Contains(scope)))
        {
            throw new InvalidOperationException("Requested scope is not allowed for this node.");
        }
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}

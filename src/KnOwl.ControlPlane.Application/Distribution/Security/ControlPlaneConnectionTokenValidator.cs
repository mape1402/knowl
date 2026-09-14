using System.Text.Json;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Default Control Plane bearer-token validator.
/// </summary>
public sealed class ControlPlaneConnectionTokenValidator(
    IRuntimeNodeRepository runtimeNodes,
    ITokenHashService tokenHashService,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionTokenCacheKeyBuilder cacheKeyBuilder,
    IDistributedCache cache) : IControlPlaneConnectionTokenValidator
{
    /// <inheritdoc />
    public async Task<ConnectionTokenValidationResult> Validate(
        string token,
        Guid runtimeNodeId,
        IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is required.");
        }

        var tokenHash = tokenHashService.HashToken(token);
        var validationKey = cacheKeyBuilder.BuildValidationKey("control-plane", tokenHash);
        var cachedValidation = await cache.GetStringAsync(validationKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedValidation))
        {
            var cachedPrincipal = JsonSerializer.Deserialize<ConnectionTokenPrincipal>(cachedValidation, JsonOptions());
            if (cachedPrincipal is not null &&
                string.Equals(cachedPrincipal.NodeId, runtimeNodeId.ToString("N"), StringComparison.OrdinalIgnoreCase) &&
                HasRequiredScopes(cachedPrincipal.Scopes, requiredScopes))
            {
                return ConnectionTokenValidationResult.Success(cachedPrincipal);
            }
        }

        var tokenJson = await cache.GetStringAsync(cacheKeyBuilder.BuildIssuedTokenKey("control-plane", tokenHash), cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenJson))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is invalid or expired.");
        }

        var entry = JsonSerializer.Deserialize<ConnectionTokenCacheEntry>(tokenJson, JsonOptions());
        if (entry is null || entry.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is invalid or expired.");
        }

        if (!Guid.TryParseExact(entry.NodeId, "N", out var tokenNodeId) || tokenNodeId != runtimeNodeId)
        {
            return ConnectionTokenValidationResult.Failure("Bearer token does not belong to the requested runtime node.");
        }

        var node = await runtimeNodes.GetById(runtimeNodeId, cancellationToken);
        if (node is null || node.IsDeleted || !node.IsEnabled || node.Status != RuntimeNodeStatus.Active || node.InboundCredentialStatus != ConnectionCredentialStatus.Active)
        {
            return ConnectionTokenValidationResult.Failure("Bearer token credential is not active.");
        }

        if (!string.Equals(node.InboundClientId, entry.ClientId, StringComparison.Ordinal) ||
            !string.Equals(node.InboundKeyId, entry.KeyId, StringComparison.Ordinal))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token credential does not match the current node key.");
        }

        var scopes = scopeFormatter.ParseMany(entry.Scopes);
        if (!HasRequiredScopes(scopes, requiredScopes))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token does not include the required scope.");
        }

        var principal = new ConnectionTokenPrincipal
        {
            NodeId = entry.NodeId,
            NodeKey = entry.NodeKey,
            ClientId = entry.ClientId,
            KeyId = entry.KeyId,
            Scopes = scopes
        };

        var validationTtl = TimeSpan.FromSeconds(Math.Max(1, node.TokenValidationCacheTtlSeconds));
        var remaining = entry.ExpiresAtUtc - DateTime.UtcNow;
        await cache.SetStringAsync(
            validationKey,
            JsonSerializer.Serialize(principal, JsonOptions()),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining < validationTtl ? remaining : validationTtl },
            cancellationToken);

        return ConnectionTokenValidationResult.Success(principal);
    }

    private static bool HasRequiredScopes(IReadOnlyCollection<ArtifactDeliveryScope> grantedScopes, IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes)
    {
        if (requiredScopes.Count == 0)
        {
            return true;
        }

        return requiredScopes.All(grantedScopes.Contains);
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}

using System.Text.Json;
using KnOwl.Contracts.Security;
using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.Extensions.Caching.Distributed;

namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Default Runtime bearer-token validator.
/// </summary>
public sealed class RuntimeConnectionTokenValidator(
    IRuntimeDesignNodeRepository designNodes,
    ITokenHashService tokenHashService,
    IConnectionScopeFormatter scopeFormatter,
    ConnectionTokenCacheKeyBuilder cacheKeyBuilder,
    IDistributedCache cache) : IRuntimeConnectionTokenValidator
{
    /// <inheritdoc />
    public async Task<ConnectionTokenValidationResult> Validate(
        string token,
        IReadOnlyCollection<ArtifactDeliveryScope> requiredScopes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is required.");
        }

        var tokenHash = tokenHashService.HashToken(token);
        var validationKey = cacheKeyBuilder.BuildValidationKey("runtime", tokenHash);
        var cachedValidation = await cache.GetStringAsync(validationKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedValidation))
        {
            var cachedPrincipal = JsonSerializer.Deserialize<ConnectionTokenPrincipal>(cachedValidation, JsonOptions());
            if (cachedPrincipal is not null && HasRequiredScopes(cachedPrincipal.Scopes, requiredScopes))
            {
                return ConnectionTokenValidationResult.Success(cachedPrincipal);
            }
        }

        var tokenJson = await cache.GetStringAsync(cacheKeyBuilder.BuildIssuedTokenKey("runtime", tokenHash), cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenJson))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is invalid or expired.");
        }

        var entry = JsonSerializer.Deserialize<ConnectionTokenCacheEntry>(tokenJson, JsonOptions());
        if (entry is null || entry.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ConnectionTokenValidationResult.Failure("Bearer token is invalid or expired.");
        }

        if (!Guid.TryParseExact(entry.NodeId, "N", out var nodeId))
        {
            return ConnectionTokenValidationResult.Failure("Bearer token node id is invalid.");
        }

        var node = await designNodes.GetById(nodeId, cancellationToken);
        if (node is null || node.Status != RuntimeDesignNodeStatus.Enabled || !node.IsEnabled || node.InboundCredentialStatus != ConnectionCredentialStatus.Active)
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

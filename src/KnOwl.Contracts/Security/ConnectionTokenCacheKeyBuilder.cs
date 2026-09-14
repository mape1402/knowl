namespace KnOwl.Contracts.Security;

/// <summary>
/// Builds cache keys used by Control Plane and Runtime connection tokens.
/// </summary>
public sealed class ConnectionTokenCacheKeyBuilder
{
    /// <summary>
    /// Builds the server-side opaque token cache key.
    /// </summary>
    public string BuildIssuedTokenKey(string issuer, string tokenHash)
        => $"knowl:async-contracts:connections:{issuer}:issued:{tokenHash}";

    /// <summary>
    /// Builds the positive validation cache key.
    /// </summary>
    public string BuildValidationKey(string issuer, string tokenHash)
        => $"knowl:async-contracts:connections:{issuer}:validation:{tokenHash}";

    /// <summary>
    /// Builds the consumer-side token cache key.
    /// </summary>
    public string BuildConsumerTokenKey(string consumer, string nodeId, string scopes)
        => $"knowl:async-contracts:connections:{consumer}:consumer:{nodeId}:{scopes}";
}

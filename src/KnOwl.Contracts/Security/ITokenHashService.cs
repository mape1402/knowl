namespace KnOwl.Contracts.Security;

/// <summary>
/// Computes stable hashes for opaque access token cache keys.
/// </summary>
public interface ITokenHashService
{
    /// <summary>
    /// Computes a SHA-256 Base64Url hash for a token.
    /// </summary>
    string HashToken(string token);
}

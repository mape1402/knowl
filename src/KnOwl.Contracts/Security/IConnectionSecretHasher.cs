namespace KnOwl.Contracts.Security;

/// <summary>
/// Hashes and verifies long-lived connection secrets.
/// </summary>
public interface IConnectionSecretHasher
{
    /// <summary>
    /// Hashes one secret using a non-reversible format.
    /// </summary>
    string HashSecret(string secret);

    /// <summary>
    /// Verifies a plain secret against a stored hash.
    /// </summary>
    bool VerifySecret(string secret, string storedHash);
}

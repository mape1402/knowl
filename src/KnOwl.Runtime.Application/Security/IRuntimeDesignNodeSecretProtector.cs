namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Protects outbound Control Plane secrets stored by the Runtime host.
/// </summary>
public interface IRuntimeDesignNodeSecretProtector
{
    /// <summary>
    /// Protects a plain secret for storage.
    /// </summary>
    string Protect(string secret);

    /// <summary>
    /// Restores a protected secret.
    /// </summary>
    string Unprotect(string protectedSecret);
}

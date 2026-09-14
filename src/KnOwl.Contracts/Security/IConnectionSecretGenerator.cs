namespace KnOwl.Contracts.Security;

/// <summary>
/// Generates long-lived connection credentials and opaque access tokens.
/// </summary>
public interface IConnectionSecretGenerator
{
    /// <summary>
    /// Generates one connection credential.
    /// </summary>
    ConnectionCredentialMaterial GenerateCredential(string prefix);

    /// <summary>
    /// Generates an opaque access token.
    /// </summary>
    string GenerateToken();
}

namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Protects outbound Runtime node secrets stored by the Control Plane.
/// </summary>
public interface IControlPlaneRuntimeNodeSecretProtector
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

using KnOwl.Security.Subjects;

namespace KnOwl.Security;

/// <summary>
/// Configures provider-agnostic KnOwl security behavior.
/// </summary>
public sealed class KnOwlSecurityOptions
{
    /// <summary>
    /// Requires every authenticated principal to be known by KnOwl before any permission can be granted.
    /// </summary>
    public bool RequireKnownSubject { get; set; }

    /// <summary>
    /// Allows configured bootstrap administrators to be synchronized into storage.
    /// </summary>
    public bool AllowBootstrapAdminSync { get; set; } = true;

    /// <summary>
    /// Claim-based subject resolution configuration.
    /// </summary>
    public KnOwlSubjectResolverOptions Subject { get; } = new();

    /// <summary>
    /// External subjects that receive administrator access for first-run and recovery scenarios.
    /// </summary>
    public List<KnOwlBootstrapSubject> BootstrapAdmins { get; } = [];
}

namespace KnOwl.Security.Storage;

/// <summary>
/// Describes where a KnOwl authorization assignment came from.
/// </summary>
public enum KnOwlSecurityAssignmentSource
{
    /// <summary>Assignment was created manually inside KnOwl.</summary>
    Manual = 0,

    /// <summary>Assignment was synchronized from bootstrap configuration.</summary>
    BootstrapConfig = 1,

    /// <summary>Assignment was inferred from an external group mapping.</summary>
    ExternalGroup = 2
}

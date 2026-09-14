namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Represents a Control Plane credential import request in Runtime.
/// </summary>
public sealed class ImportRuntimeDesignNodeCredentialPackageInput
{
    /// <summary>
    /// Gets or sets the runtime-local design node id.
    /// </summary>
    public Guid DesignNodeId { get; set; }

    /// <summary>
    /// Gets or sets the JSON or Base64 package to import.
    /// </summary>
    public string Package { get; set; } = string.Empty;
}

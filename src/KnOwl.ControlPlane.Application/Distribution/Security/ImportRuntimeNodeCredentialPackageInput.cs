namespace KnOwl.ControlPlane.Application.Distribution.Security;

/// <summary>
/// Represents a runtime node credential import request.
/// </summary>
public sealed class ImportRuntimeNodeCredentialPackageInput
{
    /// <summary>
    /// Gets or sets the runtime node id.
    /// </summary>
    public Guid RuntimeNodeId { get; set; }

    /// <summary>
    /// Gets or sets the JSON or Base64 package to import.
    /// </summary>
    public string Package { get; set; } = string.Empty;
}

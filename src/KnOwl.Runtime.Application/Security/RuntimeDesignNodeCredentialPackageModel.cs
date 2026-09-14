namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Represents a generated Control Plane credential package from Runtime.
/// </summary>
public sealed class RuntimeDesignNodeCredentialPackageModel
{
    /// <summary>
    /// Gets or sets the readable JSON package.
    /// </summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Base64 encoded package.
    /// </summary>
    public string Base64 { get; set; } = string.Empty;
}

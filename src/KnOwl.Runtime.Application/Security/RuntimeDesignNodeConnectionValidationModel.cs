namespace KnOwl.Runtime.Application.Security;

/// <summary>
/// Represents a Control Plane connection validation result from Runtime.
/// </summary>
public sealed class RuntimeDesignNodeConnectionValidationModel
{
    /// <summary>
    /// Gets or sets a value indicating whether validation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the validation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

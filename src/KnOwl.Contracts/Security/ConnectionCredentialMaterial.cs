namespace KnOwl.Contracts.Security;

/// <summary>
/// Represents newly generated connection credential material.
/// </summary>
public sealed class ConnectionCredentialMaterial
{
    /// <summary>
    /// Gets or sets the public client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated secret shown once to the operator.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the credential key identifier used for rotation and audit.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
}

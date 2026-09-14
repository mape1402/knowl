namespace KnOwl.Runtime.Distribution;

/// <summary>
/// Describes one Control Plane source known by the Runtime host.
/// </summary>
public sealed class ControlPlaneDistributionSource
{
    /// <summary>
    /// Gets or sets the runtime-local source key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Control Plane endpoint base URI.
    /// </summary>
    public string EndpointBaseUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the runtime node id assigned by the Control Plane.
    /// </summary>
    public string RemoteRuntimeNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbound client id used by Runtime to call Control Plane.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protected outbound secret.
    /// </summary>
    public string ProtectedSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outbound credential key id.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scopes requested by Runtime when calling Control Plane.
    /// </summary>
    public string RequestedScopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how early outbound tokens should be refreshed.
    /// </summary>
    public int TokenRefreshSkewSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether this source is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}

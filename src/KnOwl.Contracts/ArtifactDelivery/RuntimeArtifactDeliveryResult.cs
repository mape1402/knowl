namespace KnOwl.Contracts.ArtifactDelivery;

/// <summary>
/// Result of a Runtime artifact delivery operation.
/// </summary>
public sealed class RuntimeArtifactDeliveryResult
{
    /// <summary>
    /// Gets or sets whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the release target identifier.
    /// </summary>
    public Guid ReleaseTargetId { get; set; }

    /// <summary>
    /// Gets or sets the runtime node identifier as known by the source Control Plane.
    /// </summary>
    public Guid RuntimeNodeId { get; set; }

    /// <summary>
    /// Gets or sets the delivered artifact identifier.
    /// </summary>
    public Guid ArtifactId { get; set; }

    /// <summary>
    /// Gets or sets the current release target status after the operation.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable operation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Runtime artifact identifier reported by the Runtime host.
    /// </summary>
    public string RuntimeArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external Runtime artifact reference.
    /// </summary>
    public string ExternalReference { get; set; } = string.Empty;
}

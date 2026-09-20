using KnOwl.Contracts.Artifacts;
namespace KnOwl.Contracts.ArtifactDelivery;

/// <summary>
/// Package returned to or pushed into a Runtime node for one release target.
/// </summary>
public sealed class RuntimeArtifactDeliveryPackage
{
    /// <summary>
    /// Gets or sets the release target identifier assigned by the source Control Plane.
    /// </summary>
    public Guid ReleaseTargetId { get; set; }

    /// <summary>
    /// Gets or sets the release identifier that owns the target.
    /// </summary>
    public Guid ReleaseId { get; set; }

    /// <summary>
    /// Gets or sets the runtime node assigned to receive this package.
    /// </summary>
    public Guid RuntimeNodeId { get; set; }

    /// <summary>
    /// Gets or sets the artifact identifier inside KnOwl Distribution.
    /// </summary>
    public Guid ArtifactId { get; set; }

    /// <summary>
    /// Gets or sets the artifact type, such as Event or Command.
    /// </summary>
    public ContractArtifactType ArtifactType { get; set; }

    /// <summary>
    /// Gets or sets the artifact contract schema version.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the target runtime environment key.
    /// </summary>
    public string EnvironmentKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source contract definition identifier.
    /// </summary>
    public Guid DefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the source contract version identifier.
    /// </summary>
    public Guid VersionId { get; set; }

    /// <summary>
    /// Gets or sets the contract topic.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract version number.
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the immutable payload schema JSON snapshot.
    /// </summary>
    public string PayloadSchemaJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash used to detect conflicting artifacts.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery correlation identifier.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actor that promoted or distributed the artifact.
    /// </summary>
    public string PromotedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the artifact was promoted or distributed.
    /// </summary>
    public DateTime PromotedAtUtc { get; set; } = DateTime.UtcNow;
}

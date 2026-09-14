using KnOwl.Contracts.Artifacts;
namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents delivery of one release artifact item to one runtime node.
/// </summary>
public sealed class ContractReleaseTarget
{
    /// <summary>
    /// Release target identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Release that owns this target.
    /// </summary>
    public Guid ReleaseId { get; set; }

    /// <summary>
    /// Release item assigned to the runtime node.
    /// </summary>
    public Guid ReleaseItemId { get; set; }

    /// <summary>
    /// Runtime node assigned to receive or pull the artifact.
    /// </summary>
    public Guid RuntimeNodeId { get; set; }

    /// <summary>
    /// Artifact assigned to this runtime node.
    /// </summary>
    public Guid ArtifactId { get; set; }

    /// <summary>
    /// Release rollout group or strategy name.
    /// </summary>
    public string RolloutGroup { get; set; } = string.Empty;

    /// <summary>
    /// Current delivery status for this target.
    /// </summary>
    public ContractReleaseTargetStatus Status { get; set; } = ContractReleaseTargetStatus.Pending;

    /// <summary>
    /// Current runtime activation status for this target.
    /// </summary>
    public ContractReleaseActivationStatus ActivationStatus { get; set; } = ContractReleaseActivationStatus.NotActivated;

    /// <summary>
    /// UTC timestamp when the target was assigned.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the artifact became available for pull.
    /// </summary>
    public DateTime? AvailableAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when push delivery succeeded.
    /// </summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when runtime acknowledged the artifact.
    /// </summary>
    public DateTime? AcknowledgedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when runtime activated the artifact.
    /// </summary>
    public DateTime? ActivatedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when delivery failed.
    /// </summary>
    public DateTime? FailedAtUtc { get; set; }

    /// <summary>
    /// Failure details from delivery or activation.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Runtime artifact identifier or version applied by the runtime host.
    /// </summary>
    public string RuntimeVersionApplied { get; set; } = string.Empty;

    /// <summary>
    /// Correlation identifier for tracing release delivery.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Parent release navigation.
    /// </summary>
    public ContractRelease? Release { get; set; }

    /// <summary>
    /// Parent release item navigation.
    /// </summary>
    public ContractReleaseItem? ReleaseItem { get; set; }

    /// <summary>
    /// Target runtime node navigation.
    /// </summary>
    public RuntimeNode? RuntimeNode { get; set; }

    /// <summary>
    /// Target artifact navigation.
    /// </summary>
    public ContractArtifact? Artifact { get; set; }

    /// <summary>
    /// Delivery attempts recorded for this target.
    /// </summary>
    public ICollection<ContractReleaseAttempt> Attempts { get; set; } = [];
}

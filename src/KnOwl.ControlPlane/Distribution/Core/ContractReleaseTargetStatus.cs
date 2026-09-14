namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents delivery status for one artifact targeted to one runtime node.
/// </summary>
public enum ContractReleaseTargetStatus
{
    /// <summary>
    /// Target exists but no delivery action has been selected yet.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Target artifact is available for runtime pull.
    /// </summary>
    AvailableForPull = 2,

    /// <summary>
    /// Target artifact is scheduled for push delivery.
    /// </summary>
    PushScheduled = 3,

    /// <summary>
    /// Target delivery is in progress.
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// Target artifact was delivered to the runtime endpoint.
    /// </summary>
    Delivered = 5,

    /// <summary>
    /// Runtime acknowledged receiving the artifact.
    /// </summary>
    Acknowledged = 6,

    /// <summary>
    /// Runtime activated the artifact.
    /// </summary>
    Activated = 7,

    /// <summary>
    /// Target delivery failed.
    /// </summary>
    Failed = 8,

    /// <summary>
    /// Target delivery was cancelled.
    /// </summary>
    Cancelled = 9
}

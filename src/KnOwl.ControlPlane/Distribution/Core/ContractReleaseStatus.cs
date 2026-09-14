namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents distribution states for KnOwl contract releases.
/// </summary>
public enum ContractReleaseStatus
{
    /// <summary>
    /// Release is assembled and ready to be delivered to runtime.
    /// </summary>
    Draft,

    /// <summary>
    /// Release target creation or delivery is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Release delivery completed for all assigned targets.
    /// </summary>
    Completed,

    /// <summary>
    /// Release delivery failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Legacy state kept only to read release rows created before KnOwl release flow was simplified.
    /// </summary>
    InReview,

    /// <summary>
    /// Legacy state kept only to read release rows created before KnOwl release flow was simplified.
    /// </summary>
    Approved,

    /// <summary>
    /// Release has been deployed to runtime storage.
    /// </summary>
    Deployed,

    /// <summary>
    /// Release was canceled before deployment.
    /// </summary>
    Canceled
}

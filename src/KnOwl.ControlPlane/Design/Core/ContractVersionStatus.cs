namespace KnOwl.ControlPlane.Design.Core;

/// <summary>
/// Represents lifecycle states for deployable event and command versions.
/// </summary>
public enum ContractVersionStatus
{
    /// <summary>
    /// Version is editable and not yet submitted for review.
    /// </summary>
    Draft,

    /// <summary>
    /// Version is waiting for review.
    /// </summary>
    InReview,

    /// <summary>
    /// Version is approved and can be deployed.
    /// </summary>
    Approved,

    /// <summary>
    /// Version has been deployed to runtime storage.
    /// </summary>
    Deployed,

    /// <summary>
    /// Version is deployed but should no longer be used for new integrations.
    /// </summary>
    Deprecated,

    /// <summary>
    /// Version is terminally retained for audit/history and cannot transition further.
    /// </summary>
    Archived
}

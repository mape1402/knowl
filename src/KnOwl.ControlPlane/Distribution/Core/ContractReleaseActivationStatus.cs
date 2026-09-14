namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents activation state for a delivered runtime artifact.
/// </summary>
public enum ContractReleaseActivationStatus
{
    /// <summary>
    /// Runtime has not attempted activation.
    /// </summary>
    NotActivated = 1,

    /// <summary>
    /// Runtime activation is in progress.
    /// </summary>
    Activating = 2,

    /// <summary>
    /// Runtime activated the artifact.
    /// </summary>
    Activated = 3,

    /// <summary>
    /// Runtime activation failed.
    /// </summary>
    ActivationFailed = 4
}

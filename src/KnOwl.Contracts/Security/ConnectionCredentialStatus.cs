namespace KnOwl.Contracts.Security;

/// <summary>
/// Represents the lifecycle status of a persisted node credential.
/// </summary>
public enum ConnectionCredentialStatus
{
    /// <summary>
    /// No credential has been configured.
    /// </summary>
    Missing = 0,

    /// <summary>
    /// The credential can issue or request tokens.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The credential is disabled without losing audit history.
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// The credential has been revoked and cannot be used again.
    /// </summary>
    Revoked = 3
}

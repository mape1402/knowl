namespace KnOwl.Contracts.Security;

/// <summary>
/// Defines scopes used by KnOwl Control Plane and Runtime artifact distribution calls.
/// </summary>
public enum ArtifactDeliveryScope
{
    /// <summary>
    /// Allows the Control Plane to push artifacts into a Runtime node.
    /// </summary>
    ArtifactPush = 1,

    /// <summary>
    /// Allows a Runtime node to read release targets exposed by the Control Plane.
    /// </summary>
    ReleaseRead = 2,

    /// <summary>
    /// Allows a Runtime node to read artifact packages exposed by the Control Plane.
    /// </summary>
    ArtifactRead = 3,

    /// <summary>
    /// Allows a Runtime node to acknowledge pulled artifacts back to the Control Plane.
    /// </summary>
    ArtifactAcknowledge = 4,

    /// <summary>
    /// Allows either side to validate a configured connection.
    /// </summary>
    ConnectionValidate = 5
}

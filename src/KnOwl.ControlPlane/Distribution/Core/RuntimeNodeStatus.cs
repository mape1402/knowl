namespace KnOwl.ControlPlane.Distribution.Core;

/// <summary>
/// Represents availability status for a runtime node.
/// </summary>
public enum RuntimeNodeStatus
{
    /// <summary>
    /// Runtime node can receive or pull artifacts.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Runtime node is temporarily disabled.
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// Runtime node is no longer trusted for distribution.
    /// </summary>
    Revoked = 3
}

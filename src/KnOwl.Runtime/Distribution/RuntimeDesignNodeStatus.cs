namespace KnOwl.Runtime.Distribution;

/// <summary>
/// Represents the runtime-side configuration status of a trusted Control Plane node.
/// </summary>
public enum RuntimeDesignNodeStatus
{
    /// <summary>
    /// The node exists but its required connection configuration is incomplete.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The node is configured and can be used by runtime distribution flows.
    /// </summary>
    Enabled = 2,

    /// <summary>
    /// The node is configured but temporarily suspended.
    /// </summary>
    Suspended = 3
}

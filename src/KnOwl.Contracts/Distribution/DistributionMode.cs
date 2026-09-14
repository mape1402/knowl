namespace KnOwl.Contracts.Distribution;

/// <summary>
/// Represents whether a runtime node receives artifacts by push, pull, or both.
/// </summary>
public enum DistributionMode
{
    /// <summary>
    /// Distribution pushes artifacts to the runtime node endpoint.
    /// </summary>
    Push = 1,

    /// <summary>
    /// Runtime node pulls available artifacts from Distribution.
    /// </summary>
    Pull = 2,

    /// <summary>
    /// Distribution schedules push while keeping the node compatible with pull semantics.
    /// </summary>
    Hybrid = 3
}

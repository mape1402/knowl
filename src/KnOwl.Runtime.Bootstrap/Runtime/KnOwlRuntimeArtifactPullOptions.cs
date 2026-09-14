namespace KnOwl.Runtime.Bootstrap.Runtime;

/// <summary>
/// Configures the Runtime background pull worker.
/// </summary>
public sealed class KnOwlRuntimeArtifactPullOptions
{
    /// <summary>
    /// Enables automatic pull from configured Control Plane sources.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Seconds to wait before the first pull cycle.
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Seconds between pull cycles.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;
}

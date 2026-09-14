namespace KnOwl.Runtime.Application.ArtifactDelivery;

/// <summary>
/// Summarizes one automatic pull execution cycle on a Runtime host.
/// </summary>
public sealed class ControlPlaneArtifactPullExecutionResult
{
    /// <summary>
    /// Number of Control Plane sources scanned.
    /// </summary>
    public int SourcesScanned { get; set; }

    /// <summary>
    /// Number of packages found across all sources.
    /// </summary>
    public int PackagesFound { get; set; }

    /// <summary>
    /// Number of packages stored and acknowledged successfully.
    /// </summary>
    public int Applied { get; set; }

    /// <summary>
    /// Number of packages that failed deployment or acknowledgement.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Failure messages captured during the cycle.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];
}

namespace KnOwl.Runtime.Application.ArtifactDelivery;

/// <summary>
/// Executes automatic Runtime pull cycles for all configured Control Plane sources.
/// </summary>
public interface IControlPlaneArtifactPullOrchestrator
{
    /// <summary>
    /// Pulls and applies all packages currently available from configured Control Plane sources.
    /// </summary>
    Task<ControlPlaneArtifactPullExecutionResult> PullAvailable(CancellationToken cancellationToken = default);
}

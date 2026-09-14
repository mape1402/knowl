using KnOwl.Contracts.ArtifactDelivery;

namespace KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;

/// <summary>
/// Summarizes release target execution results.
/// </summary>
public sealed class ContractReleaseExecutionResult
{
    /// <summary>
    /// Release identifier.
    /// </summary>
    public Guid ReleaseId { get; set; }

    /// <summary>
    /// Number of targets evaluated by the execution flow.
    /// </summary>
    public int TotalTargets { get; set; }

    /// <summary>
    /// Number of targets that accepted a delivery action.
    /// </summary>
    public int Succeeded { get; set; }

    /// <summary>
    /// Number of targets that failed delivery.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Number of targets left available for runtime pull.
    /// </summary>
    public int AvailableForPull { get; set; }

    /// <summary>
    /// Detailed target results.
    /// </summary>
    public IReadOnlyList<RuntimeArtifactDeliveryResult> Results { get; set; } = [];
}

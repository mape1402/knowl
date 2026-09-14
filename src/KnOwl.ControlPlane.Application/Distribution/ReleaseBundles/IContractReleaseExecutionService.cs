using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;

/// <summary>
/// Creates release bundles and executes delivery against selected runtime nodes.
/// </summary>
public interface IContractReleaseExecutionService
{
    /// <summary>
    /// Creates a release, assigns targets and executes each target according to its distribution mode.
    /// </summary>
    Task<ContractReleaseExecutionResult> CreateAndExecute(
        string name,
        string? description,
        IReadOnlyCollection<Guid> artifactIds,
        IReadOnlyCollection<Guid> runtimeNodeIds,
        string rolloutGroup = "ManualRelease",
        string initiatedBy = "distribution",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes pending targets for an existing release.
    /// </summary>
    Task<ContractReleaseExecutionResult> Execute(
        Guid releaseId,
        string initiatedBy = "distribution",
        CancellationToken cancellationToken = default);
}

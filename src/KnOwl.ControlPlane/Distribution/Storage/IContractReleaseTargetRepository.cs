using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Defines persistence operations for release targets assigned to runtime nodes.
/// </summary>
public interface IContractReleaseTargetRepository
{
    /// <summary>
    /// Gets targets for a release.
    /// </summary>
    Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets targets pending pull for a runtime node.
    /// </summary>
    Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one target by identifier.
    /// </summary>
    Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists release targets.
    /// </summary>
    Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a release target status and operational timestamps.
    /// </summary>
    Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a distribution attempt for a release target.
    /// </summary>
    Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default);
}

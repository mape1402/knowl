using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Defines persistence operations for contract release bundles.
/// </summary>
public interface IContractReleaseRepository
{
    /// <summary>
    /// Gets all releases with their selected artifacts.
    /// </summary>
    Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a release by identifier.
    /// </summary>
    Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new release bundle.
    /// </summary>
    Task Create(ContractRelease release, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates release lifecycle status.
    /// </summary>
    Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default);
}

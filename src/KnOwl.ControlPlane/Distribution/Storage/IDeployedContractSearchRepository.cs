using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Provides storage-side searching for bounded deployed-contract summaries.
/// </summary>
public interface IDeployedContractSearchRepository
{
    /// <summary>
    /// Searches deployed contract summaries without loading their schema documents.
    /// </summary>
    Task<IReadOnlyList<DeployedContractSearchResult>> SearchDeployed(
        DeployedContractSearchQuery query,
        CancellationToken cancellationToken = default);
}

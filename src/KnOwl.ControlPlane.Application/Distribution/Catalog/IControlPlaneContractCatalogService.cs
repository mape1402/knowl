using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Application.Distribution.Catalog;

/// <summary>
/// Provides read access to deployed contract artifacts stored in KnOwl Control Plane.
/// </summary>
public interface IControlPlaneContractCatalogService
{
    /// <summary>
    /// Searches bounded summaries of deployed contracts without returning their schema documents.
    /// </summary>
    Task<IReadOnlyList<DeployedContractSearchResult>> Search(
        DeployedContractSearchQuery query,
        CancellationToken cancellationToken = default)
        => Task.FromException<IReadOnlyList<DeployedContractSearchResult>>(
            new NotSupportedException("The configured Control Plane contract catalog does not support deployed-contract search."));

    /// <summary>
    /// Gets all deployed contract artifacts.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one deployed artifact by type, contract key and exact version number.
    /// </summary>
    Task<ContractArtifact?> GetExact(
        ContractArtifactType artifactType,
        string contractKey,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest deployed artifact by type and contract key.
    /// </summary>
    Task<ContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string contractKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one deployed event artifact by key and exact version number.
    /// </summary>
    Task<ContractArtifact?> GetEvent(
        string eventKey,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployed command request and optional reply artifacts by key and exact version number.
    /// </summary>
    Task<CommandContractArtifacts<ContractArtifact>?> GetCommand(
        string commandKey,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets latest deployed command request and optional reply artifacts by key.
    /// </summary>
    Task<CommandContractArtifacts<ContractArtifact>?> GetLatestCommand(
        string commandKey,
        CancellationToken cancellationToken = default);
}

using KnOwl.Contracts.Artifacts;

namespace KnOwl.ControlPlane.Application.Distribution.Catalog;

/// <summary>
/// Provides read access to deployed contract artifacts stored in KnOwl Control Plane.
/// </summary>
public interface IControlPlaneContractCatalogService
{
    /// <summary>
    /// Gets all deployed contract artifacts.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one deployed artifact by type, topic and exact version number.
    /// </summary>
    Task<ContractArtifact?> GetExact(
        ContractArtifactType artifactType,
        string topic,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest deployed artifact by type and topic.
    /// </summary>
    Task<ContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string topic,
        CancellationToken cancellationToken = default);
}

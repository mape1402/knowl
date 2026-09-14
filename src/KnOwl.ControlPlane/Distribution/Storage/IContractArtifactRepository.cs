using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;

namespace KnOwl.ControlPlane.Distribution.Storage;

/// <summary>
/// Defines persistence operations for immutable contract artifacts.
/// </summary>
public interface IContractArtifactRepository
{
    /// <summary>
    /// Gets all contract artifacts ordered by creation date.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all deployed contract artifacts ordered by creation date.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contract artifact by identifier.
    /// </summary>
    Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets artifacts matching the provided identifiers.
    /// </summary>
    Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contract artifact by source version.
    /// </summary>
    Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contract artifact by runtime identity.
    /// </summary>
    Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a deployed contract artifact by runtime identity.
    /// </summary>
    Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest deployed artifact for a contract topic.
    /// </summary>
    Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new immutable contract artifact.
    /// </summary>
    Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default);
}


using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;

namespace KnOwl.Runtime.Storage;

/// <summary>
/// Defines persistence operations for runtime contract artifacts.
/// </summary>
public interface IRuntimeContractArtifactRepository
{
    /// <summary>
    /// Gets all runtime contract artifacts.
    /// </summary>
    Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a runtime artifact by runtime identity.
    /// </summary>
    Task<RuntimeContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest deployed runtime artifact for a contract topic.
    /// </summary>
    Task<RuntimeContractArtifact?> GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a runtime contract artifact.
    /// </summary>
    Task Create(RuntimeContractArtifact artifact, CancellationToken cancellationToken = default);
}

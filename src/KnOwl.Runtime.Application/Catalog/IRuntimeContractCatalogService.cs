using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;

namespace KnOwl.Runtime.Application.Catalog;

/// <summary>
/// Provides read access to contract artifacts stored in KnOwl Runtime.
/// </summary>
public interface IRuntimeContractCatalogService
{
    /// <summary>
    /// Gets all artifacts stored in runtime.
    /// </summary>
    Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one artifact by type, topic and exact version number.
    /// </summary>
    Task<RuntimeContractArtifact?> GetExact(
        ContractArtifactType artifactType,
        string topic,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest deployed artifact by type and topic.
    /// </summary>
    Task<RuntimeContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string topic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one event artifact by key and exact version number.
    /// </summary>
    Task<RuntimeContractArtifact?> GetEvent(
        string eventKey,
        string versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets command request and optional reply artifacts by key and exact version number.
    /// </summary>
    Task<CommandContractArtifacts<RuntimeContractArtifact>?> GetCommand(
        string commandKey,
        string versionNumber,
        CancellationToken cancellationToken = default);
}

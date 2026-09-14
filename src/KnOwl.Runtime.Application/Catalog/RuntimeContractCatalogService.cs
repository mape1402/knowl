using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Storage;

namespace KnOwl.Runtime.Application.Catalog;

/// <inheritdoc />
internal sealed class RuntimeContractCatalogService(IRuntimeContractArtifactRepository runtimeArtifacts) : IRuntimeContractCatalogService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default)
        => runtimeArtifacts.GetAll(cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeContractArtifact?> GetExact(
        ContractArtifactType artifactType,
        string topic,
        string versionNumber,
        CancellationToken cancellationToken = default)
        => runtimeArtifacts.GetByIdentity(artifactType, topic, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string topic,
        CancellationToken cancellationToken = default)
        => runtimeArtifacts.GetLatest(artifactType, topic, cancellationToken);
}

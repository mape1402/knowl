using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Storage;

namespace KnOwl.ControlPlane.Application.Distribution.Catalog;

/// <inheritdoc />
internal sealed class ControlPlaneContractCatalogService(IContractArtifactRepository artifacts) : IControlPlaneContractCatalogService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default)
        => artifacts.GetDeployed(cancellationToken);

    /// <inheritdoc />
    public Task<ContractArtifact?> GetExact(
        ContractArtifactType artifactType,
        string topic,
        string versionNumber,
        CancellationToken cancellationToken = default)
        => artifacts.GetDeployedByIdentity(artifactType, topic, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task<ContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string topic,
        CancellationToken cancellationToken = default)
        => artifacts.GetLatestDeployed(artifactType, topic, cancellationToken);
}

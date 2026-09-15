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

    /// <inheritdoc />
    public Task<ContractArtifact?> GetEvent(
        string eventKey,
        string versionNumber,
        CancellationToken cancellationToken = default)
        => artifacts.GetDeployedByIdentity(ContractArtifactType.Event, eventKey, versionNumber, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandContractArtifacts<ContractArtifact>?> GetCommand(
        string commandKey,
        string versionNumber,
        CancellationToken cancellationToken = default)
    {
        var request = await artifacts.GetDeployedByIdentity(ContractArtifactType.CommandRequest, commandKey, versionNumber, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var reply = await artifacts.GetDeployedByIdentity(ContractArtifactType.CommandReply, commandKey, versionNumber, cancellationToken);
        return new CommandContractArtifacts<ContractArtifact>(commandKey, versionNumber, request, reply);
    }
}

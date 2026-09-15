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

    /// <inheritdoc />
    public Task<RuntimeContractArtifact?> GetEvent(
        string eventKey,
        string versionNumber,
        CancellationToken cancellationToken = default)
        => runtimeArtifacts.GetByIdentity(ContractArtifactType.Event, eventKey, versionNumber, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandContractArtifacts<RuntimeContractArtifact>?> GetCommand(
        string commandKey,
        string versionNumber,
        CancellationToken cancellationToken = default)
    {
        var request = await runtimeArtifacts.GetByIdentity(ContractArtifactType.CommandRequest, commandKey, versionNumber, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var reply = await runtimeArtifacts.GetByIdentity(ContractArtifactType.CommandReply, commandKey, versionNumber, cancellationToken);
        return new CommandContractArtifacts<RuntimeContractArtifact>(commandKey, versionNumber, request, reply);
    }
}

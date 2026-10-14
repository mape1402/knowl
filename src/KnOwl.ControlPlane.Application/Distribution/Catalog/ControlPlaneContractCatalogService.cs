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
        string contractKey,
        string versionNumber,
        CancellationToken cancellationToken = default)
        => artifacts.GetDeployedByIdentity(artifactType, contractKey, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task<ContractArtifact?> GetLatest(
        ContractArtifactType artifactType,
        string contractKey,
        CancellationToken cancellationToken = default)
        => artifacts.GetLatestDeployed(artifactType, contractKey, cancellationToken);

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
        var command = await artifacts.GetDeployedByIdentity(ContractArtifactType.Command, commandKey, versionNumber, cancellationToken);
        if (command is not null)
        {
            return ToCommandArtifacts(command);
        }

        var request = await artifacts.GetDeployedByIdentity(ContractArtifactType.CommandRequest, commandKey, versionNumber, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var reply = await artifacts.GetDeployedByIdentity(ContractArtifactType.CommandReply, commandKey, versionNumber, cancellationToken);
        return new CommandContractArtifacts<ContractArtifact>(commandKey, versionNumber, request, reply);
    }

    /// <inheritdoc />
    public async Task<CommandContractArtifacts<ContractArtifact>?> GetLatestCommand(
        string commandKey,
        CancellationToken cancellationToken = default)
    {
        var command = await artifacts.GetLatestDeployed(ContractArtifactType.Command, commandKey, cancellationToken);
        if (command is not null)
        {
            return ToCommandArtifacts(command);
        }

        var request = await artifacts.GetLatestDeployed(ContractArtifactType.CommandRequest, commandKey, cancellationToken);
        if (request is null)
        {
            return null;
        }

        var reply = await artifacts.GetDeployedByIdentity(ContractArtifactType.CommandReply, commandKey, request.VersionNumber, cancellationToken);
        return new CommandContractArtifacts<ContractArtifact>(commandKey, request.VersionNumber, request, reply);
    }

    private static CommandContractArtifacts<ContractArtifact> ToCommandArtifacts(ContractArtifact command)
    {
        var payload = CommandArtifactPayloadDocument.Read(command.PayloadSchemaJson);
        var request = CreateCommandPart(command, ContractArtifactType.CommandRequest, "Request", payload.RequestPayloadSchemaJson);
        var reply = string.IsNullOrWhiteSpace(payload.ReplyPayloadSchemaJson)
            ? null
            : CreateCommandPart(command, ContractArtifactType.CommandReply, "Reply", payload.ReplyPayloadSchemaJson);

        return new CommandContractArtifacts<ContractArtifact>(command.ContractKey, command.VersionNumber, request, reply);
    }

    private static ContractArtifact CreateCommandPart(
        ContractArtifact command,
        ContractArtifactType artifactType,
        string partName,
        string payloadSchemaJson)
        => new()
        {
            Id = command.Id,
            ArtifactType = artifactType,
            DefinitionId = command.DefinitionId,
            VersionId = command.VersionId,
            Name = $"{command.Name} {partName}",
            ContractKey = command.ContractKey,
            VersionNumber = command.VersionNumber,
            Description = command.Description,
            PayloadSchemaJson = payloadSchemaJson,
            ContentHash = command.ContentHash,
            SourceStatus = command.SourceStatus,
            CreatedAtUtc = command.CreatedAtUtc
        };
}

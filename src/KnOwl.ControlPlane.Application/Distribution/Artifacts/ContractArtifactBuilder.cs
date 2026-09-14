using KnOwl.Contracts.Artifacts;
using System.Security.Cryptography;
using System.Text;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application.Distribution.Artifacts;

/// <inheritdoc />
internal sealed class ContractArtifactBuilder(
    IEventRepository events,
    ICommandRepository commands,
    IContractArtifactRepository artifacts,
    IContractSnapshotValidationService snapshotValidator) : IContractArtifactBuilder
{
    /// <inheritdoc />
    public async Task<ContractArtifact> BuildEventArtifact(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await events.GetVersionById(versionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event version '{versionId}' was not found.");
        var definition = version.EventDefinition
            ?? throw new InvalidOperationException($"Event version '{versionId}' does not include its definition snapshot.");

        EnsureArtifactStatus(version.Status, versionId);
        await EnsureValidSnapshot(versionId, ContractArtifactType.Event, cancellationToken);

        return await GetOrCreateArtifact(new ContractArtifact
        {
            ArtifactType = ContractArtifactType.Event,
            DefinitionId = version.EventDefinitionId,
            VersionId = version.Id,
            Name = definition.Name,
            Topic = definition.Topic,
            VersionNumber = version.VersionNumber,
            Description = definition.Description,
            PayloadSchemaJson = version.PayloadSchemaJson,
            SourceStatus = version.Status.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact> BuildCommandArtifact(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await commands.GetVersionById(versionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Command version '{versionId}' was not found.");
        var definition = version.CommandDefinition
            ?? throw new InvalidOperationException($"Command version '{versionId}' does not include its definition snapshot.");

        EnsureArtifactStatus(version.Status, versionId);
        await EnsureValidSnapshot(versionId, ContractArtifactType.Command, cancellationToken);

        return await GetOrCreateArtifact(new ContractArtifact
        {
            ArtifactType = ContractArtifactType.Command,
            DefinitionId = version.CommandDefinitionId,
            VersionId = version.Id,
            Name = definition.Name,
            Topic = definition.Topic,
            VersionNumber = version.VersionNumber,
            Description = definition.Description,
            PayloadSchemaJson = version.PayloadSchemaJson,
            SourceStatus = version.Status.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task<ContractArtifact> GetOrCreateArtifact(ContractArtifact artifact, CancellationToken cancellationToken)
    {
        artifact.ContentHash = ComputeContentHash(artifact);

        var existingSource = await artifacts.GetBySourceVersion(artifact.ArtifactType, artifact.VersionId, cancellationToken);
        if (existingSource is not null)
        {
            if (!string.Equals(existingSource.ContentHash, artifact.ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Artifact source version '{artifact.VersionId}' already exists with a different hash.");
            }

            return existingSource;
        }

        var existingIdentity = await artifacts.GetByIdentity(artifact.ArtifactType, artifact.Topic, artifact.VersionNumber, cancellationToken);
        if (existingIdentity is not null && !string.Equals(existingIdentity.ContentHash, artifact.ContentHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Artifact '{artifact.ArtifactType}:{artifact.Topic}@{artifact.VersionNumber}' already exists with a different hash.");
        }

        if (existingIdentity is not null)
        {
            return existingIdentity;
        }

        await artifacts.Create(artifact, cancellationToken);
        return artifact;
    }

    private async Task EnsureValidSnapshot(Guid versionId, ContractArtifactType artifactType, CancellationToken cancellationToken)
    {
        var validation = artifactType == ContractArtifactType.Event
            ? await snapshotValidator.ValidateEventVersion(versionId, cancellationToken)
            : await snapshotValidator.ValidateCommandVersion(versionId, cancellationToken);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Contract snapshot is not valid: {string.Join("; ", validation.Errors)}");
        }
    }

    private static void EnsureArtifactStatus(ContractVersionStatus status, Guid versionId)
    {
        if (status is ContractVersionStatus.Approved or ContractVersionStatus.Deployed)
        {
            return;
        }

        throw new InvalidOperationException($"Version '{versionId}' must be Approved or Deployed before an artifact can be generated. Current status: {status}.");
    }

    private static string ComputeContentHash(ContractArtifact artifact)
    {
        var canonical = string.Join('\n',
            artifact.ArtifactType,
            artifact.DefinitionId,
            artifact.VersionId,
            artifact.Topic.Trim(),
            artifact.VersionNumber.Trim(),
            artifact.PayloadSchemaJson.Trim());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}


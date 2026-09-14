using KnOwl.Contracts.ArtifactDelivery;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Storage;

namespace KnOwl.Runtime.Application.Deployments;

/// <inheritdoc />
internal sealed class RuntimeContractDeploymentService(IRuntimeContractArtifactRepository runtimeArtifacts) : IRuntimeContractDeploymentService
{
    /// <inheritdoc />
    public async Task<RuntimeArtifactDeploymentResult> DeployArtifact(
        RuntimeArtifactDeliveryPackage package,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (string.IsNullOrWhiteSpace(package.Topic))
        {
            return Rejected("Artifact topic is required.");
        }

        if (string.IsNullOrWhiteSpace(package.VersionNumber))
        {
            return Rejected("Artifact version number is required.");
        }

        if (string.IsNullOrWhiteSpace(package.ContentHash))
        {
            return Rejected("Artifact content hash is required.");
        }

        var existing = await runtimeArtifacts.GetByIdentity(package.ArtifactType, package.Topic, package.VersionNumber, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.ContentHash, package.ContentHash, StringComparison.Ordinal))
            {
                return Rejected($"Runtime artifact '{package.ArtifactType}:{package.Topic}@{package.VersionNumber}' already exists with a different hash.");
            }

            return new RuntimeArtifactDeploymentResult
            {
                Accepted = true,
                RuntimeArtifactId = existing.Id.ToString("N"),
                Status = "Ready",
                Message = $"Artifact '{package.Topic}' v{package.VersionNumber} is already ready."
            };
        }

        var artifact = new RuntimeContractArtifact
        {
            Id = Guid.NewGuid(),
            SourceArtifactId = package.ArtifactId,
            SourceReleaseId = package.ReleaseId,
            ArtifactType = package.ArtifactType,
            DefinitionId = package.DefinitionId,
            VersionId = package.VersionId,
            Name = package.Name,
            Topic = package.Topic,
            VersionNumber = package.VersionNumber,
            Description = package.Description,
            PayloadSchemaJson = package.PayloadSchemaJson,
            ContentHash = package.ContentHash,
            DeployedAtUtc = DateTime.UtcNow
        };

        await runtimeArtifacts.Create(artifact, cancellationToken);
        return new RuntimeArtifactDeploymentResult
        {
            Accepted = true,
            RuntimeArtifactId = artifact.Id.ToString("N"),
            Status = "Ready",
            Message = $"Artifact '{artifact.Topic}' v{artifact.VersionNumber} stored from '{sourceKey}'."
        };
    }

    private static RuntimeArtifactDeploymentResult Rejected(string message)
        => new()
        {
            Accepted = false,
            RuntimeArtifactId = string.Empty,
            Status = "Rejected",
            Message = message
        };
}

using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractArtifactRepository(KnOwlDbContext db) : IContractArtifactRepository, IDeployedContractSearchRepository
{
    private static readonly string DeployedStatus = ContractVersionStatus.Deployed.ToString();

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractArtifact>> GetDeployed(CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .Where(x => x.SourceStatus == DeployedStatus)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeployedContractSearchResult>> SearchDeployed(
        DeployedContractSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var deployedArtifacts = ApplyTextFilter(
            db.ContractArtifacts
                .AsNoTracking()
                .Where(x => x.SourceStatus == DeployedStatus && x.PayloadSchemaJson != string.Empty),
            query.NormalizedText);

        IQueryable<ContractArtifact> results = query.ArtifactType switch
        {
            ContractArtifactType.Event => deployedArtifacts.Where(x => x.ArtifactType == ContractArtifactType.Event),
            ContractArtifactType.Command => LogicalCommands(deployedArtifacts),
            ContractArtifactType.CommandRequest => deployedArtifacts.Where(x => x.ArtifactType == ContractArtifactType.CommandRequest),
            ContractArtifactType.CommandReply => deployedArtifacts.Where(x => x.ArtifactType == ContractArtifactType.CommandReply),
            null => deployedArtifacts.Where(x => x.ArtifactType == ContractArtifactType.Event)
                .Concat(LogicalCommands(deployedArtifacts)),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.ArtifactType, "The artifact type is not supported.")
        };
        var collapseLegacyRequests = query.ArtifactType is null or ContractArtifactType.Command;

        return await results
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.ContractKey.ToUpper())
            .ThenBy(x => x.ContractKey)
            .ThenBy(x => x.VersionNumber.ToUpper())
            .ThenBy(x => x.VersionNumber)
            .ThenBy(x => x.Id)
            .Take(query.EffectiveLimit)
            .Select(x => new DeployedContractSearchResult(
                x.Id,
                collapseLegacyRequests && x.ArtifactType == ContractArtifactType.CommandRequest
                    ? ContractArtifactType.Command
                    : x.ArtifactType,
                x.ContractKey,
                x.VersionNumber,
                x.ContentHash,
                x.CreatedAtUtc,
                true))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractArtifact>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact?> GetBySourceVersion(ContractArtifactType artifactType, Guid versionId, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactType == artifactType && x.VersionId == versionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string contractKey, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactType == artifactType && x.ContractKey == contractKey && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact?> GetDeployedByIdentity(ContractArtifactType artifactType, string contractKey, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SourceStatus == DeployedStatus &&
                    x.ArtifactType == artifactType &&
                    x.ContractKey == contractKey &&
                    x.VersionNumber == versionNumber,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractArtifact?> GetLatestDeployed(ContractArtifactType artifactType, string contractKey, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .Where(x => x.SourceStatus == DeployedStatus && x.ArtifactType == artifactType && x.ContractKey == contractKey)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default)
    {
        db.ContractArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ContractArtifact> LogicalCommands(IQueryable<ContractArtifact> deployedArtifacts)
    {
        var modernCommands = deployedArtifacts.Where(x => x.ArtifactType == ContractArtifactType.Command);

        var legacyRequestRepresentatives = deployedArtifacts
            .Where(x => x.ArtifactType == ContractArtifactType.CommandRequest)
            .Where(request => !db.ContractArtifacts.Any(command =>
                command.SourceStatus == DeployedStatus &&
                command.ArtifactType == ContractArtifactType.Command &&
                command.ContractKey.ToUpper() == request.ContractKey.ToUpper() &&
                command.VersionNumber.ToUpper() == request.VersionNumber.ToUpper()));

        return modernCommands.Concat(legacyRequestRepresentatives);
    }

    private static IQueryable<ContractArtifact> ApplyTextFilter(IQueryable<ContractArtifact> query, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return query;
        }

        var normalized = text.ToUpperInvariant();
        if (Guid.TryParse(text, out var identifier))
        {
            return query.Where(x =>
                x.Id == identifier ||
                x.DefinitionId == identifier ||
                x.VersionId == identifier ||
                x.ContractKey.ToUpper().Contains(normalized) ||
                x.VersionNumber.ToUpper().Contains(normalized) ||
                x.ContentHash.ToUpper().Contains(normalized));
        }

        return query.Where(x =>
            x.ContractKey.ToUpper().Contains(normalized) ||
            x.VersionNumber.ToUpper().Contains(normalized) ||
            x.ContentHash.ToUpper().Contains(normalized));
    }

}


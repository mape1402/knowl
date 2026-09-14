using KnOwl.Contracts.Artifacts;
using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractArtifactRepository(KnOwlDbContext db) : IContractArtifactRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractArtifact>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
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
    public async Task<ContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.ContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(ContractArtifact artifact, CancellationToken cancellationToken = default)
    {
        db.ContractArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
    }
}


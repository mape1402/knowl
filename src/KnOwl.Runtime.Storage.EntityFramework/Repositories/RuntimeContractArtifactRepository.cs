using KnOwl.Contracts.Artifacts;
using KnOwl.Runtime.Core;
using KnOwl.Runtime.Storage;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Runtime.Storage.EntityFramework.Repositories;

/// <inheritdoc />
public sealed class RuntimeContractArtifactRepository(KnOwlRuntimeDbContext db) : IRuntimeContractArtifactRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeContractArtifact>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeContractArtifacts
            .AsNoTracking()
            .OrderByDescending(x => x.DeployedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeContractArtifact?> GetByIdentity(ContractArtifactType artifactType, string topic, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeContractArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactType == artifactType && x.Topic == topic && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeContractArtifact?> GetLatest(ContractArtifactType artifactType, string topic, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeContractArtifacts
            .AsNoTracking()
            .Where(x => x.ArtifactType == artifactType && x.Topic == topic)
            .OrderByDescending(x => x.DeployedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(RuntimeContractArtifact artifact, CancellationToken cancellationToken = default)
    {
        db.RuntimeContractArtifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
    }
}

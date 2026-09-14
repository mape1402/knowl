using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractReleaseTargetRepository(KnOwlDbContext db) : IContractReleaseTargetRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractReleaseTarget>> GetByRelease(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return await db.ContractReleaseTargets
            .AsNoTracking()
            .Include(x => x.RuntimeNode)
            .Include(x => x.Artifact)
            .Include(x => x.Attempts)
            .Where(x => x.ReleaseId == releaseId)
            .OrderBy(x => x.RuntimeNode!.EnvironmentName)
            .ThenBy(x => x.RuntimeNode!.Name)
            .ThenBy(x => x.Artifact!.Topic)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractReleaseTarget>> GetPendingForRuntimeNode(Guid runtimeNodeId, CancellationToken cancellationToken = default)
    {
        return await db.ContractReleaseTargets
            .AsNoTracking()
            .Include(x => x.Artifact)
            .Include(x => x.RuntimeNode)
            .Where(x => x.RuntimeNodeId == runtimeNodeId &&
                (x.Status == ContractReleaseTargetStatus.AvailableForPull || x.Status == ContractReleaseTargetStatus.PushScheduled))
            .OrderBy(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractReleaseTarget?> GetById(Guid id, bool includeArtifact = false, CancellationToken cancellationToken = default)
    {
        var query = db.ContractReleaseTargets.AsQueryable();
        if (includeArtifact)
        {
            query = query.Include(x => x.Artifact).Include(x => x.RuntimeNode);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreateMany(IReadOnlyCollection<ContractReleaseTarget> targets, CancellationToken cancellationToken = default)
    {
        db.ContractReleaseTargets.AddRange(targets);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(ContractReleaseTarget target, CancellationToken cancellationToken = default)
    {
        db.ContractReleaseTargets.Update(target);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAttempt(ContractReleaseAttempt attempt, CancellationToken cancellationToken = default)
    {
        db.ContractReleaseAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
    }
}

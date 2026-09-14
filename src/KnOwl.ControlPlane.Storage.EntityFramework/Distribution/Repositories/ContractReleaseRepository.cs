using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class ContractReleaseRepository(KnOwlDbContext db) : IContractReleaseRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractRelease>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.ContractReleases
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(x => x.Artifact)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractRelease?> GetById(Guid id, bool includeItems = false, bool includeTargets = false, CancellationToken cancellationToken = default)
    {
        var query = db.ContractReleases.AsQueryable();
        if (includeItems || includeTargets)
        {
            query = query.AsSplitQuery();
        }

        if (includeItems)
        {
            query = query.Include(x => x.Items).ThenInclude(x => x.Artifact);
        }
        if (includeTargets)
        {
            query = query.Include(x => x.Targets).ThenInclude(x => x.RuntimeNode)
                .Include(x => x.Targets).ThenInclude(x => x.Artifact)
                .Include(x => x.Targets).ThenInclude(x => x.Attempts);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(ContractRelease release, CancellationToken cancellationToken = default)
    {
        db.ContractReleases.Add(release);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateStatus(Guid id, ContractReleaseStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
    {
        var query = db.ContractReleases.Where(x => x.Id == id);
        var rows = status switch
        {
            ContractReleaseStatus.Deployed => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.DeployedAtUtc, changedAtUtc), cancellationToken),
            ContractReleaseStatus.InProgress => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status), cancellationToken),
            ContractReleaseStatus.Completed => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.CompletedAtUtc, changedAtUtc), cancellationToken),
            ContractReleaseStatus.Failed => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.FailedAtUtc, changedAtUtc), cancellationToken),
            ContractReleaseStatus.Canceled => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.CanceledAtUtc, changedAtUtc), cancellationToken),
            _ => await query.ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, status), cancellationToken)
        };

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Release '{id}' was not found.");
        }
    }
}

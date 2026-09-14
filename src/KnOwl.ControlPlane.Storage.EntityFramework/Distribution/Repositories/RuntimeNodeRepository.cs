using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class RuntimeNodeRepository(KnOwlDbContext db) : IRuntimeNodeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeNode>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .AsNoTracking()
            .Include(x => x.Environment)
            .OrderBy(x => x.Environment != null ? x.Environment.Name : x.EnvironmentName)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeNode>> GetActiveEnabled(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .AsNoTracking()
            .Include(x => x.Environment)
            .Where(x => x.IsEnabled && x.Status == RuntimeNodeStatus.Active)
            .OrderBy(x => x.Environment != null ? x.Environment.Name : x.EnvironmentName)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes.Include(x => x.Environment).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .FirstOrDefaultAsync(x => x.Code == code.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeNodes
            .FirstOrDefaultAsync(x => x.InboundClientId == clientId.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
    {
        db.RuntimeNodes.Add(runtimeNode);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(RuntimeNode runtimeNode, CancellationToken cancellationToken = default)
    {
        runtimeNode.LastUpdatedAtUtc = DateTime.UtcNow;
        db.RuntimeNodes.Update(runtimeNode);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetIsEnabled(Guid id, bool isEnabled, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.RuntimeNodes
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsEnabled, isEnabled)
                .SetProperty(x => x.LastUpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Runtime node '{id}' was not found.");
        }
    }
}

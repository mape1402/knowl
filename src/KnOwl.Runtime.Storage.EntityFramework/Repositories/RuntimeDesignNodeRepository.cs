using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.Runtime.Storage.EntityFramework.Repositories;

/// <inheritdoc />
public sealed class RuntimeDesignNodeRepository(KnOwlRuntimeDbContext db) : IRuntimeDesignNodeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeDesignNode>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeDesignNodes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeDesignNode?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeDesignNodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeDesignNode?> GetByKey(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim();
        return await db.RuntimeDesignNodes.FirstOrDefaultAsync(x => x.Key == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeDesignNode?> GetByInboundClientId(string clientId, CancellationToken cancellationToken = default)
    {
        var normalized = clientId.Trim();
        return await db.RuntimeDesignNodes.FirstOrDefaultAsync(x => x.InboundClientId == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Upsert(RuntimeDesignNode designNode, CancellationToken cancellationToken = default)
    {
        designNode.UpdatedAtUtc = DateTime.UtcNow;
        var exists = await db.RuntimeDesignNodes.AnyAsync(x => x.Id == designNode.Id, cancellationToken);
        if (exists)
        {
            db.RuntimeDesignNodes.Update(designNode);
        }
        else
        {
            db.RuntimeDesignNodes.Add(designNode);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

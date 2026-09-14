using KnOwl.ControlPlane.Distribution.Core;
using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;

/// <inheritdoc />
public sealed class RuntimeEnvironmentRepository(KnOwlDbContext db) : IRuntimeEnvironmentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeEnvironment>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeEnvironments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeEnvironment>> GetEnabled(CancellationToken cancellationToken = default)
    {
        return await db.RuntimeEnvironments
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuntimeEnvironment?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.RuntimeEnvironments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
    {
        db.RuntimeEnvironments.Add(environment);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(RuntimeEnvironment environment, CancellationToken cancellationToken = default)
    {
        environment.UpdatedAtUtc = DateTime.UtcNow;
        db.RuntimeEnvironments.Update(environment);
        await db.SaveChangesAsync(cancellationToken);
    }
}

using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class LanguageTypeBundleRepository(KnOwlDbContext db) : ILanguageTypeBundleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeBundles
            .AsNoTracking()
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.AssemblyKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetActive(CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeBundles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.AssemblyKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LanguageTypeBundleDefinition?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeBundles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LanguageTypeBundleDefinition?> GetByAssemblyId(string assemblyId, CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeBundles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AssemblyId == assemblyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Upsert(LanguageTypeBundleDefinition bundle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var existing = await db.LanguageTypeBundles.FirstOrDefaultAsync(x => x.AssemblyId == bundle.AssemblyId, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is null)
        {
            bundle.Id = bundle.Id == Guid.Empty ? Guid.NewGuid() : bundle.Id;
            bundle.CreatedAtUtc = bundle.CreatedAtUtc == default ? now : bundle.CreatedAtUtc;
            bundle.UpdatedAtUtc = now;
            db.LanguageTypeBundles.Add(bundle);
        }
        else
        {
            existing.AssemblyKey = bundle.AssemblyKey;
            existing.AssemblyDescription = bundle.AssemblyDescription;
            existing.AssemblyVersion = bundle.AssemblyVersion;
            existing.LanguageKey = bundle.LanguageKey;
            existing.BundleJson = bundle.BundleJson;
            existing.IsActive = bundle.IsActive;
            existing.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var existing = await db.LanguageTypeBundles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Language type bundle '{id}' was not found.");

        existing.IsActive = isActive;
        existing.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }
}

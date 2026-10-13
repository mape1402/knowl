using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class LanguageTypeMappingRepository(KnOwlDbContext db) : ILanguageTypeMappingRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetAll(CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeMappings
            .AsNoTracking()
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetActive(CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeMappings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LanguageTypeMappingDefinition?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LanguageTypeMappingDefinition?> GetByKey(string key, CancellationToken cancellationToken = default)
    {
        return await db.LanguageTypeMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Upsert(LanguageTypeMappingDefinition mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        var existing = await db.LanguageTypeMappings.FirstOrDefaultAsync(x => x.Key == mapping.Key, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is null)
        {
            mapping.Id = mapping.Id == Guid.Empty ? Guid.NewGuid() : mapping.Id;
            mapping.CreatedAtUtc = mapping.CreatedAtUtc == default ? now : mapping.CreatedAtUtc;
            mapping.UpdatedAtUtc = now;
            db.LanguageTypeMappings.Add(mapping);
        }
        else
        {
            existing.Name = mapping.Name;
            existing.Description = mapping.Description;
            existing.LanguageKey = mapping.LanguageKey;
            existing.DocumentJson = mapping.DocumentJson;
            existing.IsActive = mapping.IsActive;
            existing.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var existing = await db.LanguageTypeMappings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Language type mapping '{id}' was not found.");

        existing.IsActive = isActive;
        existing.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }
}

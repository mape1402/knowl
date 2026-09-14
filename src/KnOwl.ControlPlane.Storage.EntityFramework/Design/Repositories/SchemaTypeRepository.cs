using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class SchemaTypeRepository(KnOwlDbContext db) : ISchemaTypeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaTypeDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default)
    {
        return await db.SchemaTypes
            .AsNoTracking()
            .Include(x => x.Versions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default)
    {
        return await db.SchemaTypeVersions
            .AsNoTracking()
            .Include(x => x.SchemaTypeDefinition)
            .Where(x => x.IsActive && x.SchemaTypeDefinition != null && x.SchemaTypeDefinition.IsActive)
            .OrderBy(x => x.SchemaTypeDefinition!.Name)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var query = db.SchemaTypes.AsQueryable();
        if (includeVersions)
        {
            query = query.Include(x => x.Versions);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await db.SchemaTypeVersions
            .AsNoTracking()
            .Include(x => x.SchemaTypeDefinition)
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await db.SchemaTypes.AnyAsync(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.SchemaTypeVersions.AnyAsync(x => x.SchemaTypeDefinitionId == typeId && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
    {
        db.SchemaTypes.Add(schemaType);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.SchemaTypes
            .Where(x => x.Id == id && !x.IsSystem)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Key, key)
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.Description, description)
                .SetProperty(x => x.IsActive, isActive)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Schema type '{id}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
    {
        var entity = await db.SchemaTypes.FirstOrDefaultAsync(x => x.Id == typeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Schema type '{typeId}' was not found.");

        version.SchemaTypeDefinitionId = typeId;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.SchemaTypeVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.SchemaTypeVersions
            .Where(x => x.Id == versionId &&
                        x.SchemaTypeDefinitionId == typeId &&
                        x.SchemaTypeDefinition != null &&
                        !x.SchemaTypeDefinition.IsSystem)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, isActive)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Schema type version '{versionId}' was not found.");
        }
    }
}


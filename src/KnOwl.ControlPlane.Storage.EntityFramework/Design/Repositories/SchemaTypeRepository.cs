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
        var schemaTypes = await db.SchemaTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        await HydrateVersions(schemaTypes, cancellationToken);
        return schemaTypes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default)
    {
        var activeDefinitions = await db.SchemaTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var definitionIds = activeDefinitions.Select(x => x.Id).ToArray();
        if (definitionIds.Length == 0)
        {
            return [];
        }

        var definitions = activeDefinitions.ToDictionary(x => x.Id);
        var versions = await db.SchemaTypeVersions
            .AsNoTracking()
            .Where(x => x.IsActive && definitionIds.Contains(x.SchemaTypeDefinitionId))
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            if (definitions.TryGetValue(version.SchemaTypeDefinitionId, out var definition))
            {
                version.SchemaTypeDefinition = definition;
            }
        }

        return versions
            .OrderBy(x => x.SchemaTypeDefinition?.Name)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var schemaType = await db.SchemaTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (includeVersions)
        {
            await HydrateVersions([schemaType], cancellationToken);
        }

        return schemaType;
    }

    /// <inheritdoc />
    public async Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await db.SchemaTypeVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        if (version is not null)
        {
            version.SchemaTypeDefinition = await db.SchemaTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == version.SchemaTypeDefinitionId, cancellationToken);
        }

        return version;
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
        var entity = await db.SchemaTypes.FirstOrDefaultAsync(x => x.Id == id && !x.IsSystem, cancellationToken);
        if (entity is null)
        {
            throw new KeyNotFoundException($"Schema type '{id}' was not found.");
        }

        entity.Key = key;
        entity.Name = name;
        entity.Description = description;
        entity.IsActive = isActive;
        entity.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
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
        var schemaType = await db.SchemaTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == typeId, cancellationToken);
        if (schemaType is null || schemaType.IsSystem)
        {
            throw new KeyNotFoundException($"Schema type version '{versionId}' was not found.");
        }

        var version = await db.SchemaTypeVersions
            .FirstOrDefaultAsync(x => x.Id == versionId &&
                                      x.SchemaTypeDefinitionId == typeId, cancellationToken);

        if (version is null)
        {
            throw new KeyNotFoundException($"Schema type version '{versionId}' was not found.");
        }

        version.IsActive = isActive;
        version.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HydrateVersions(IReadOnlyCollection<SchemaTypeDefinition?> schemaTypes, CancellationToken cancellationToken)
    {
        var definitions = schemaTypes
            .Where(x => x is not null)
            .Cast<SchemaTypeDefinition>()
            .ToArray();
        var definitionIds = definitions.Select(x => x.Id).ToArray();
        if (definitionIds.Length == 0)
        {
            return;
        }

        var versions = await db.SchemaTypeVersions
            .AsNoTracking()
            .Where(x => definitionIds.Contains(x.SchemaTypeDefinitionId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var versionsByDefinition = versions.ToLookup(x => x.SchemaTypeDefinitionId);

        foreach (var definition in definitions)
        {
            definition.Versions = versionsByDefinition[definition.Id].ToList();
        }
    }
}


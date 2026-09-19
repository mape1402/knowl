using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class ContractFieldMetadataRepository(KnOwlDbContext db) : IContractFieldMetadataRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default)
    {
        var definitions = await db.ContractFieldMetadataDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        await HydrateVersions(definitions, onlyActiveVersions: false, cancellationToken);
        return definitions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActiveWithVersions(CancellationToken cancellationToken = default)
    {
        var definitions = await db.ContractFieldMetadataDefinitions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        await HydrateVersions(definitions, onlyActiveVersions: true, cancellationToken);
        return definitions;
    }

    /// <inheritdoc />
    public async Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var definition = await db.ContractFieldMetadataDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (includeVersions)
        {
            await HydrateVersions([definition], onlyActiveVersions: false, cancellationToken);
        }

        return definition;
    }

    /// <inheritdoc />
    public async Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return await db.ContractFieldMetadataDefinitions.AnyAsync(x => x.Key == key && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.ContractFieldMetadataVersions.AnyAsync(x => x.ContractFieldMetadataDefinitionId == metadataFieldId && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default)
    {
        db.ContractFieldMetadataDefinitions.Add(metadataField);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await db.ContractFieldMetadataDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Metadata field '{id}' was not found.");

        entity.Key = key;
        entity.Name = name;
        entity.Description = description;
        entity.IsActive = isActive;
        entity.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
    {
        var current = await db.ContractFieldMetadataVersions
            .FirstOrDefaultAsync(x => x.ContractFieldMetadataDefinitionId == metadataFieldId && x.VersionNumber == version.VersionNumber, cancellationToken);

        if (current is null)
        {
            version.ContractFieldMetadataDefinitionId = metadataFieldId;
            db.ContractFieldMetadataVersions.Add(version);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        current.Comment = version.Comment;
        current.DefinitionJson = version.DefinitionJson;
        current.IsActive = version.IsActive;
        current.UpdatedAtUtc = version.UpdatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var version = await db.ContractFieldMetadataVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ContractFieldMetadataDefinitionId == metadataFieldId, cancellationToken);

        if (version is null)
        {
            throw new KeyNotFoundException($"Metadata field version '{versionId}' was not found.");
        }

        version.IsActive = isActive;
        version.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HydrateVersions(
        IReadOnlyCollection<ContractFieldMetadataDefinition?> definitions,
        bool onlyActiveVersions,
        CancellationToken cancellationToken)
    {
        var metadataFields = definitions
            .Where(x => x is not null)
            .Cast<ContractFieldMetadataDefinition>()
            .ToArray();
        var metadataFieldIds = metadataFields.Select(x => x.Id).ToArray();
        if (metadataFieldIds.Length == 0)
        {
            return;
        }

        var query = db.ContractFieldMetadataVersions
            .AsNoTracking()
            .Where(x => metadataFieldIds.Contains(x.ContractFieldMetadataDefinitionId));
        if (onlyActiveVersions)
        {
            query = query.Where(x => x.IsActive);
        }

        var versions = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var versionsByDefinition = versions.ToLookup(x => x.ContractFieldMetadataDefinitionId);

        foreach (var metadataField in metadataFields)
        {
            metadataField.Versions = versionsByDefinition[metadataField.Id].ToList();
        }
    }
}


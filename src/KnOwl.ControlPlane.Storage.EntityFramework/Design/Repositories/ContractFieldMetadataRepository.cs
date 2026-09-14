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
        return await db.ContractFieldMetadataDefinitions
            .AsNoTracking()
            .Include(x => x.Versions)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActiveWithVersions(CancellationToken cancellationToken = default)
    {
        return await db.ContractFieldMetadataDefinitions
            .AsNoTracking()
            .Include(x => x.Versions)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var query = db.ContractFieldMetadataDefinitions.AsQueryable();
        if (includeVersions)
        {
            query = query.Include(x => x.Versions);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
        var rows = await db.ContractFieldMetadataDefinitions
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Key, key)
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.Description, description)
                .SetProperty(x => x.IsActive, isActive)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Metadata field '{id}' was not found.");
        }
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
        var rows = await db.ContractFieldMetadataVersions
            .Where(x => x.Id == versionId && x.ContractFieldMetadataDefinitionId == metadataFieldId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, isActive)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Metadata field version '{versionId}' was not found.");
        }
    }
}


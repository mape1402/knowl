using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class CommandRepository(KnOwlDbContext db) : ICommandRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default)
    {
        return await db.Commands
            .AsNoTracking()
            .Include(x => x.Versions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var query = db.Commands.AsQueryable();
        if (includeVersions)
        {
            query = query.Include(x => x.Versions);
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.CommandVersions.AnyAsync(x => x.CommandDefinitionId == commandId && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await db.CommandVersions
            .AsNoTracking()
            .Include(x => x.CommandDefinition)
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
    {
        db.Commands.Add(commandDefinition);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.Commands
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.Topic, topic)
                .SetProperty(x => x.Description, description)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Command '{id}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
    {
        var entity = await db.Commands.FirstOrDefaultAsync(x => x.Id == commandId, cancellationToken)
            ?? throw new KeyNotFoundException($"Command '{commandId}' was not found.");

        version.CommandDefinitionId = commandId;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.CommandVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
    {
        var query = db.CommandVersions.Where(x => x.Id == versionId);
        var rows = status switch
        {
            ContractVersionStatus.InReview => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc)
                .SetProperty(x => x.InReviewAtUtc, changedAtUtc), cancellationToken),
            ContractVersionStatus.Approved => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc)
                .SetProperty(x => x.ApprovedAtUtc, changedAtUtc), cancellationToken),
            ContractVersionStatus.Deployed => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc)
                .SetProperty(x => x.DeployedAtUtc, changedAtUtc), cancellationToken),
            ContractVersionStatus.Deprecated => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc)
                .SetProperty(x => x.DeprecatedAtUtc, changedAtUtc), cancellationToken),
            ContractVersionStatus.Archived => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc)
                .SetProperty(x => x.ArchivedAtUtc, changedAtUtc), cancellationToken),
            _ => await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAtUtc, changedAtUtc), cancellationToken)
        };

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Command version '{versionId}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await db.Commands
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Command '{id}' was not found.");
        }
    }

}


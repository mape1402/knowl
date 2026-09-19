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
        var commands = await db.Commands
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await HydrateVersions(commands, cancellationToken);
        return commands;
    }

    /// <inheritdoc />
    public async Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var command = await db.Commands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (includeVersions)
        {
            await HydrateVersions([command], cancellationToken);
        }

        return command;
    }

    /// <inheritdoc />
    public async Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.CommandVersions.AnyAsync(x => x.CommandDefinitionId == commandId && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await db.CommandVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        if (version is not null)
        {
            version.CommandDefinition = await db.Commands
                .AsNoTracking()
                .FirstAsync(x => x.Id == version.CommandDefinitionId, cancellationToken);
        }

        return version;
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
        var entity = await db.Commands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Command '{id}' was not found.");

        entity.Name = name;
        entity.Topic = topic;
        entity.Description = description;
        entity.UpdatedAtUtc = updatedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
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
        var version = await db.CommandVersions.FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);
        if (version is null)
        {
            throw new KeyNotFoundException($"Command version '{versionId}' was not found.");
        }

        version.Status = status;
        version.UpdatedAtUtc = changedAtUtc;
        ApplyVersionStatusTimestamp(version, status, changedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDraftVersion(Guid versionId, string requestSchemaJson, string? replySchemaJson, string? comment, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var version = await db.CommandVersions.FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);
        if (version is null)
        {
            throw new KeyNotFoundException($"Command version '{versionId}' was not found.");
        }

        if (version.Status != ContractVersionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft command versions can be edited.");
        }

        version.PayloadSchemaJson = requestSchemaJson;
        version.ReplyPayloadSchemaJson = string.IsNullOrWhiteSpace(replySchemaJson) ? null : replySchemaJson;
        version.Comment = comment;
        version.UpdatedAtUtc = updatedAtUtc;

        var command = await db.Commands.FirstOrDefaultAsync(x => x.Id == version.CommandDefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Command '{version.CommandDefinitionId}' was not found.");
        command.UpdatedAtUtc = updatedAtUtc;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Commands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Command '{id}' was not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyVersionStatusTimestamp(CommandVersion version, ContractVersionStatus status, DateTime changedAtUtc)
    {
        switch (status)
        {
            case ContractVersionStatus.InReview:
                version.InReviewAtUtc = changedAtUtc;
                break;
            case ContractVersionStatus.Approved:
                version.ApprovedAtUtc = changedAtUtc;
                break;
            case ContractVersionStatus.Deployed:
                version.DeployedAtUtc = changedAtUtc;
                break;
            case ContractVersionStatus.Deprecated:
                version.DeprecatedAtUtc = changedAtUtc;
                break;
            case ContractVersionStatus.Archived:
                version.ArchivedAtUtc = changedAtUtc;
                break;
        }
    }

    private async Task HydrateVersions(IReadOnlyCollection<CommandDefinition?> commands, CancellationToken cancellationToken)
    {
        var definitions = commands
            .Where(x => x is not null)
            .Cast<CommandDefinition>()
            .ToArray();
        var commandIds = definitions.Select(x => x.Id).ToArray();
        if (commandIds.Length == 0)
        {
            return;
        }

        var versions = await db.CommandVersions
            .AsNoTracking()
            .Where(x => commandIds.Contains(x.CommandDefinitionId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var versionsByCommand = versions.ToLookup(x => x.CommandDefinitionId);

        foreach (var command in definitions)
        {
            command.Versions = versionsByCommand[command.Id].ToList();
        }
    }
}


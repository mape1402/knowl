using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;

/// <inheritdoc />
public sealed class EventRepository(KnOwlDbContext db) : IEventRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default)
    {
        var events = await db.Events
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await HydrateVersions(events, cancellationToken);
        return events;
    }

    /// <inheritdoc />
    public async Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
    {
        var eventDefinition = await db.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (includeVersions)
        {
            await HydrateVersions([eventDefinition], cancellationToken);
        }

        return eventDefinition;
    }

    /// <inheritdoc />
    public async Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await db.EventVersions.AnyAsync(x => x.EventDefinitionId == eventId && x.VersionNumber == versionNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await db.EventVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        if (version is not null)
        {
            version.EventDefinition = await db.Events
                .AsNoTracking()
                .FirstAsync(x => x.Id == version.EventDefinitionId, cancellationToken);
        }

        return version;
    }

    /// <inheritdoc />
    public async Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default)
    {
        db.Events.Add(eventDefinition);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        var rows = await db.Events
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, name)
                .SetProperty(x => x.Topic, topic)
                .SetProperty(x => x.Description, description)
                .SetProperty(x => x.UpdatedAtUtc, updatedAtUtc), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Event '{id}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default)
    {
        var entity = await db.Events.FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event '{eventId}' was not found.");

        version.EventDefinitionId = eventId;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.EventVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default)
    {
        var query = db.EventVersions.Where(x => x.Id == versionId);
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
            throw new KeyNotFoundException($"Event version '{versionId}' was not found.");
        }
    }

    /// <inheritdoc />
    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await db.Events
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);

        if (rows == 0)
        {
            throw new KeyNotFoundException($"Event '{id}' was not found.");
        }
    }

    private async Task HydrateVersions(IReadOnlyCollection<EventDefinition?> events, CancellationToken cancellationToken)
    {
        var definitions = events
            .Where(x => x is not null)
            .Cast<EventDefinition>()
            .ToArray();
        var eventIds = definitions.Select(x => x.Id).ToArray();
        if (eventIds.Length == 0)
        {
            return;
        }

        var versions = await db.EventVersions
            .AsNoTracking()
            .Where(x => eventIds.Contains(x.EventDefinitionId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var versionsByEvent = versions.ToLookup(x => x.EventDefinitionId);

        foreach (var eventDefinition in definitions)
        {
            eventDefinition.Versions = versionsByEvent[eventDefinition.Id].ToList();
        }
    }
}


using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for event definitions and their versioned payload contracts.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Gets all event definitions including their versions.
    /// </summary>
    Task<IReadOnlyList<EventDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an event definition by identifier, optionally including version history.
    /// </summary>
    Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the event already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an event version by identifier.
    /// </summary>
    Task<EventVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new event definition with its initial state.
    /// </summary>
    Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global event definition data without changing version payloads.
    /// </summary>
    Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new version to an existing event definition.
    /// </summary>
    Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates lifecycle status for an event version.
    /// </summary>
    Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event definition inactive without deleting historical versions.
    /// </summary>
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

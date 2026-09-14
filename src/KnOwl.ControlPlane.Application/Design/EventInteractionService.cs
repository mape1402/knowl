using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class EventInteractionService(IEventRepository repository) : IEventInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<EventDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAllWithVersions(cancellationToken);

    /// <inheritdoc />
    public Task<EventDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        => repository.GetById(id, includeVersions, cancellationToken);

    /// <inheritdoc />
    public Task<bool> VersionExists(Guid eventId, string versionNumber, CancellationToken cancellationToken = default)
        => repository.VersionExists(eventId, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task Create(EventDefinition eventDefinition, CancellationToken cancellationToken = default)
        => repository.Create(eventDefinition, cancellationToken);

    /// <inheritdoc />
    public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.UpdateDefinition(id, name, topic, description, updatedAtUtc, cancellationToken);

    /// <inheritdoc />
    public Task AddVersion(Guid eventId, EventVersion version, CancellationToken cancellationToken = default)
        => repository.AddVersion(eventId, version, cancellationToken);

    /// <inheritdoc />
    public Task Delete(Guid id, CancellationToken cancellationToken = default)
        => repository.Delete(id, cancellationToken);
}


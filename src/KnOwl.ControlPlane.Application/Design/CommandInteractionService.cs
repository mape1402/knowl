using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class CommandInteractionService(ICommandRepository repository) : ICommandInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAllWithVersions(cancellationToken);

    /// <inheritdoc />
    public Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        => repository.GetById(id, includeVersions, cancellationToken);

    /// <inheritdoc />
    public Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default)
        => repository.VersionExists(commandId, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default)
        => repository.Create(commandDefinition, cancellationToken);

    /// <inheritdoc />
    public Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.UpdateDefinition(id, name, topic, description, updatedAtUtc, cancellationToken);

    /// <inheritdoc />
    public Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default)
        => repository.AddVersion(commandId, version, cancellationToken);

    /// <inheritdoc />
    public Task Delete(Guid id, CancellationToken cancellationToken = default)
        => repository.Delete(id, cancellationToken);
}


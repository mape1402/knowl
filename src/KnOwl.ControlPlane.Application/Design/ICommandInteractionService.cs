using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates command contract workflows for the web UI and designer adapters.
/// </summary>
public interface ICommandInteractionService
{
    /// <summary>
    /// Gets all command definitions including version history.
    /// </summary>
    Task<IReadOnlyList<CommandDefinition>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a command definition by identifier, optionally including version history.
    /// </summary>
    Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the command already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new command definition.
    /// </summary>
    Task Create(CommandDefinition commandDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global command definition data without changing version payloads.
    /// </summary>
    Task UpdateDefinition(Guid id, string name, string topic, string? description, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new version to an existing command definition.
    /// </summary>
    Task AddVersion(Guid commandId, CommandVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a command definition inactive without deleting historical versions.
    /// </summary>
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for command definitions and their versioned payload contracts.
/// </summary>
public interface ICommandRepository
{
    /// <summary>
    /// Gets all command definitions including their versions.
    /// </summary>
    Task<IReadOnlyList<CommandDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a command definition by identifier, optionally including version history.
    /// </summary>
    Task<CommandDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the command already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid commandId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a command version by identifier.
    /// </summary>
    Task<CommandVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new command definition with its initial state.
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
    /// Updates lifecycle status for a command version.
    /// </summary>
    Task UpdateVersionStatus(Guid versionId, ContractVersionStatus status, DateTime changedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a command definition inactive without deleting historical versions.
    /// </summary>
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates schema type catalog workflows and version selection.
/// </summary>
public interface ISchemaTypeInteractionService
{
    /// <summary>
    /// Gets all schema type definitions including version history.
    /// </summary>
    Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active schema type versions for selectors and schema builders.
    /// </summary>
    Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema type definition by identifier, optionally including version history.
    /// </summary>
    Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema type version by identifier.
    /// </summary>
    Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a schema type key is already in use.
    /// </summary>
    Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the schema type already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new schema type definition.
    /// </summary>
    Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global schema type data without changing historical versions.
    /// </summary>
    Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new version to an existing schema type definition.
    /// </summary>
    Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the active state of a schema type version without deleting it.
    /// </summary>
    Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

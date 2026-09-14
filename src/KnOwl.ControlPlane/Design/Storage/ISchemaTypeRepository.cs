using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for system and custom schema type definitions.
/// </summary>
public interface ISchemaTypeRepository
{
    /// <summary>
    /// Gets all schema type definitions including their versions.
    /// </summary>
    Task<IReadOnlyList<SchemaTypeDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active type versions with their owning definitions for selector catalogs.
    /// </summary>
    Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersionsWithDefinitions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema type definition by identifier, optionally including version history.
    /// </summary>
    Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a schema type version by identifier.
    /// </summary>
    Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a type key is already in use.
    /// </summary>
    Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the type already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new schema type definition with its initial version data.
    /// </summary>
    Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global schema type data without mutating historical versions.
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

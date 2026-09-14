using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for configurable field metadata definitions.
/// </summary>
public interface IContractFieldMetadataRepository
{
    /// <summary>
    /// Gets all metadata field definitions including version history.
    /// </summary>
    Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAllWithVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active metadata field definitions with active versions for dynamic forms.
    /// </summary>
    Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActiveWithVersions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a metadata field definition by identifier, optionally including version history.
    /// </summary>
    Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a metadata key is already in use.
    /// </summary>
    Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the metadata field already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new metadata field definition with its initial version data.
    /// </summary>
    Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global metadata field data without mutating historical versions.
    /// </summary>
    Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a metadata field version for the owning definition.
    /// </summary>
    Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the active state of a metadata field version without deleting it.
    /// </summary>
    Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

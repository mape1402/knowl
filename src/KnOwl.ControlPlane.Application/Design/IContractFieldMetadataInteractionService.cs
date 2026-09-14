using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates custom field metadata workflows and dynamic form catalogs.
/// </summary>
public interface IContractFieldMetadataInteractionService
{
    /// <summary>
    /// Gets all custom field metadata definitions including version history.
    /// </summary>
    Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active custom field metadata definitions for dynamic payload forms.
    /// </summary>
    Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom field metadata definition by identifier, optionally including version history.
    /// </summary>
    Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a custom field metadata key is already in use.
    /// </summary>
    Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the custom field metadata already contains the requested version number.
    /// </summary>
    Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom field metadata definition.
    /// </summary>
    Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates global custom field metadata data without changing historical versions.
    /// </summary>
    Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a custom field metadata version.
    /// </summary>
    Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the active state of a custom field metadata version without deleting it.
    /// </summary>
    Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

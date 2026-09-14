using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class ContractFieldMetadataInteractionService(IContractFieldMetadataRepository repository) : IContractFieldMetadataInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAllWithVersions(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContractFieldMetadataDefinition>> GetActive(CancellationToken cancellationToken = default)
        => repository.GetActiveWithVersions(cancellationToken);

    /// <inheritdoc />
    public Task<ContractFieldMetadataDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        => repository.GetById(id, includeVersions, cancellationToken);

    /// <inheritdoc />
    public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => repository.KeyExists(key, excludingId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> VersionExists(Guid metadataFieldId, string versionNumber, CancellationToken cancellationToken = default)
        => repository.VersionExists(metadataFieldId, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task Create(ContractFieldMetadataDefinition metadataField, CancellationToken cancellationToken = default)
        => repository.Create(metadataField, cancellationToken);

    /// <inheritdoc />
    public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.UpdateDefinition(id, key, name, description, isActive, updatedAtUtc, cancellationToken);

    /// <inheritdoc />
    public Task UpsertVersion(Guid metadataFieldId, ContractFieldMetadataVersion version, CancellationToken cancellationToken = default)
        => repository.UpsertVersion(metadataFieldId, version, cancellationToken);

    /// <inheritdoc />
    public Task SetVersionActive(Guid metadataFieldId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.SetVersionActive(metadataFieldId, versionId, isActive, updatedAtUtc, cancellationToken);
}


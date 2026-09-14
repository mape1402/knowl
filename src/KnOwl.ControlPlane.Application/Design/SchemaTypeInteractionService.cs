using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class SchemaTypeInteractionService(ISchemaTypeRepository repository) : ISchemaTypeInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<SchemaTypeDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAllWithVersions(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<SchemaTypeVersion>> GetActiveVersions(CancellationToken cancellationToken = default)
        => repository.GetActiveVersionsWithDefinitions(cancellationToken);

    /// <inheritdoc />
    public Task<SchemaTypeDefinition?> GetById(Guid id, bool includeVersions = false, CancellationToken cancellationToken = default)
        => repository.GetById(id, includeVersions, cancellationToken);

    /// <inheritdoc />
    public Task<SchemaTypeVersion?> GetVersionById(Guid versionId, CancellationToken cancellationToken = default)
        => repository.GetVersionById(versionId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> KeyExists(string key, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => repository.KeyExists(key, excludingId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> VersionExists(Guid typeId, string versionNumber, CancellationToken cancellationToken = default)
        => repository.VersionExists(typeId, versionNumber, cancellationToken);

    /// <inheritdoc />
    public Task Create(SchemaTypeDefinition schemaType, CancellationToken cancellationToken = default)
        => repository.Create(schemaType, cancellationToken);

    /// <inheritdoc />
    public Task UpdateDefinition(Guid id, string key, string name, string? description, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.UpdateDefinition(id, key, name, description, isActive, updatedAtUtc, cancellationToken);

    /// <inheritdoc />
    public Task AddVersion(Guid typeId, SchemaTypeVersion version, CancellationToken cancellationToken = default)
        => repository.AddVersion(typeId, version, cancellationToken);

    /// <inheritdoc />
    public Task SetVersionActive(Guid typeId, Guid versionId, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.SetVersionActive(typeId, versionId, isActive, updatedAtUtc, cancellationToken);
}


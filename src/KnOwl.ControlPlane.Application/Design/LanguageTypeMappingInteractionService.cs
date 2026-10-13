using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Design.Storage;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class LanguageTypeMappingInteractionService(ILanguageTypeMappingRepository repository) : ILanguageTypeMappingInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAll(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetActive(CancellationToken cancellationToken = default)
        => repository.GetActive(cancellationToken);

    /// <inheritdoc />
    public Task<LanguageTypeMappingDefinition?> GetById(Guid id, CancellationToken cancellationToken = default)
        => repository.GetById(id, cancellationToken);

    /// <inheritdoc />
    public Task<LanguageTypeMappingDefinition?> GetByKey(string key, CancellationToken cancellationToken = default)
        => repository.GetByKey(key, cancellationToken);

    /// <inheritdoc />
    public Task Upsert(LanguageTypeMappingDefinition mapping, CancellationToken cancellationToken = default)
        => repository.Upsert(mapping, cancellationToken);

    /// <inheritdoc />
    public Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.SetActive(id, isActive, updatedAtUtc, cancellationToken);
}

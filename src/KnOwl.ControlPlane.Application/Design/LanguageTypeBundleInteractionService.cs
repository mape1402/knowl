using KnOwl.ControlPlane.Design.Core;
using KnOwl.ControlPlane.Design.Storage;

namespace KnOwl.ControlPlane.Application;

/// <inheritdoc />
internal sealed class LanguageTypeBundleInteractionService(ILanguageTypeBundleRepository repository) : ILanguageTypeBundleInteractionService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetAll(CancellationToken cancellationToken = default)
        => repository.GetAll(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetActive(CancellationToken cancellationToken = default)
        => repository.GetActive(cancellationToken);

    /// <inheritdoc />
    public Task<LanguageTypeBundleDefinition?> GetById(Guid id, CancellationToken cancellationToken = default)
        => repository.GetById(id, cancellationToken);

    /// <inheritdoc />
    public Task<LanguageTypeBundleDefinition?> GetByAssemblyId(string assemblyId, CancellationToken cancellationToken = default)
        => repository.GetByAssemblyId(assemblyId, cancellationToken);

    /// <inheritdoc />
    public Task Upsert(LanguageTypeBundleDefinition bundle, CancellationToken cancellationToken = default)
        => repository.Upsert(bundle, cancellationToken);

    /// <inheritdoc />
    public Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
        => repository.SetActive(id, isActive, updatedAtUtc, cancellationToken);
}

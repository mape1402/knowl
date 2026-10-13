using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates language type bundle workflows.
/// </summary>
public interface ILanguageTypeBundleInteractionService
{
    Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetAll(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetActive(CancellationToken cancellationToken = default);

    Task<LanguageTypeBundleDefinition?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<LanguageTypeBundleDefinition?> GetByAssemblyId(string assemblyId, CancellationToken cancellationToken = default);

    Task Upsert(LanguageTypeBundleDefinition bundle, CancellationToken cancellationToken = default);

    Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for host-owned language type bundle metadata.
/// </summary>
public interface ILanguageTypeBundleRepository
{
    Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetAll(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageTypeBundleDefinition>> GetActive(CancellationToken cancellationToken = default);

    Task<LanguageTypeBundleDefinition?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<LanguageTypeBundleDefinition?> GetByAssemblyId(string assemblyId, CancellationToken cancellationToken = default);

    Task Upsert(LanguageTypeBundleDefinition bundle, CancellationToken cancellationToken = default);

    Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

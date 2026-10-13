using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Design.Storage;

/// <summary>
/// Defines persistence operations for host-owned language type mapping documents.
/// </summary>
public interface ILanguageTypeMappingRepository
{
    Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetAll(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetActive(CancellationToken cancellationToken = default);

    Task<LanguageTypeMappingDefinition?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<LanguageTypeMappingDefinition?> GetByKey(string key, CancellationToken cancellationToken = default);

    Task Upsert(LanguageTypeMappingDefinition mapping, CancellationToken cancellationToken = default);

    Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

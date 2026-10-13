using KnOwl.ControlPlane.Design.Core;

namespace KnOwl.ControlPlane.Application;

/// <summary>
/// Coordinates language type mapping document workflows.
/// </summary>
public interface ILanguageTypeMappingInteractionService
{
    Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetAll(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LanguageTypeMappingDefinition>> GetActive(CancellationToken cancellationToken = default);

    Task<LanguageTypeMappingDefinition?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<LanguageTypeMappingDefinition?> GetByKey(string key, CancellationToken cancellationToken = default);

    Task Upsert(LanguageTypeMappingDefinition mapping, CancellationToken cancellationToken = default);

    Task SetActive(Guid id, bool isActive, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}

using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class IndexModel(IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    public IReadOnlyList<MetadataFieldListItem> Fields { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var entities = await metadataFields.GetAll(cancellationToken);

        Fields = entities
            .Select(CreateListItem)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await metadataFields.GetById(id, cancellationToken: cancellationToken);
        if (entity is not null)
        {
            await metadataFields.UpdateDefinition(
                entity.Id,
                entity.Key,
                entity.Name,
                entity.Description,
                isActive: false,
                DateTime.UtcNow,
                cancellationToken);
        }

        return RedirectToPage();
    }

    private static MetadataFieldListItem CreateListItem(ContractFieldMetadataDefinition entity)
    {
        var version = entity.Versions
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (version is null)
        {
            return new MetadataFieldListItem(
                entity.Id,
                entity.Name,
                entity.Key,
                entity.Description,
                string.Empty,
                string.Empty,
                entity.IsActive,
                false,
                [],
                entity.CreatedAtUtc);
        }

        var definition = KnOwlButterMorphDefinitionMapper.ToDefinition(entity, version);
        return new MetadataFieldListItem(
            entity.Id,
            entity.Name,
            entity.Key,
            entity.Description,
            version.VersionNumber,
            definition.DataType,
            entity.IsActive,
            definition.IsRequired,
            definition.AppliesTo,
            entity.CreatedAtUtc);
    }

    public sealed record MetadataFieldListItem(
        Guid Id,
        string Name,
        string Key,
        string? Description,
        string Version,
        string DataType,
        bool IsActive,
        bool IsRequired,
        IReadOnlyCollection<string> AppliesTo,
        DateTime CreatedAtUtc);
}

using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.MetadataFields;

public class IndexModel(IContractFieldMetadataInteractionService metadataFields) : PageModel
{
    public IReadOnlyList<MetadataFieldListItem> Fields { get; private set; } = [];
    public int TotalFields { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var entities = await metadataFields.GetAll(cancellationToken);

        Search = Normalize(Search);
        var fieldRows = entities
            .Select(CreateListItem)
            .ToArray();

        TotalFields = fieldRows.Length;
        Fields = (string.IsNullOrWhiteSpace(Search)
                ? fieldRows
                : fieldRows.Where(MatchesSearch))
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
                entity.Versions.Count,
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
            entity.Versions.Count,
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
        int VersionCount,
        DateTime CreatedAtUtc);

    private bool MatchesSearch(MetadataFieldListItem item)
        => Contains(item.Name)
            || Contains(item.Key)
            || Contains(item.Description)
            || Contains(item.Version)
            || Contains(item.DataType)
            || Contains(item.IsActive ? "Active" : "Inactive")
            || Contains(item.IsRequired ? "Required" : "Optional")
            || item.AppliesTo.Any(Contains);

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

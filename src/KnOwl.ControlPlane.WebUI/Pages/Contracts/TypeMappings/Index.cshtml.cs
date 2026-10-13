using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ButterMorphLanguageTypeMappingDocument = global::ButterMorph.Language.ButterMorphLanguageTypeMappingDocument;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.TypeMappings;

public class IndexModel(ILanguageTypeMappingInteractionService mappings) : PageModel
{
    public IReadOnlyList<TypeMappingCard> Items { get; private set; } = [];
    public int TotalItems { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var rows = await mappings.GetAll(cancellationToken);
        TotalItems = rows.Count;
        Search = Normalize(Search);
        Items = rows
            .Select(x => new TypeMappingCard(
                x.Id,
                x.Key,
                x.Name,
                x.Description,
                x.LanguageKey,
                x.IsActive,
                x.UpdatedAtUtc))
            .Where(x => string.IsNullOrWhiteSpace(Search) || MatchesSearch(x))
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.Name)
            .ToArray();
    }

    public async Task<IActionResult> OnPostSaveDocumentAsync([FromBody] ButterMorphLanguageTypeMappingDocument document, CancellationToken cancellationToken)
    {
        if (document is null)
        {
            return new JsonResult(new { succeeded = false, message = "Type mapping document is required." });
        }

        var entity = KnOwlLanguageTypeMappingHost.ToEntity(document);
        await mappings.Upsert(entity, cancellationToken);
        return new JsonResult(new { succeeded = true, message = "Type mapping saved." });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await mappings.SetActive(id, isActive: false, DateTime.UtcNow, cancellationToken);
        return RedirectToPage();
    }

    private bool MatchesSearch(TypeMappingCard item)
        => Contains(item.Key)
            || Contains(item.Name)
            || Contains(item.Description)
            || Contains(item.LanguageKey)
            || Contains(item.IsActive ? "Active" : "Inactive");

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record TypeMappingCard(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string LanguageKey,
        bool IsActive,
        DateTime UpdatedAtUtc);
}

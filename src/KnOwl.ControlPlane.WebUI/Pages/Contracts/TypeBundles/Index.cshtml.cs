using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.WebUI.ButterMorph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ButterMorphLanguageTypeBundle = global::ButterMorph.Language.ButterMorphLanguageTypeBundle;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.TypeBundles;

public class IndexModel(ILanguageTypeBundleInteractionService bundles) : PageModel
{
    public IReadOnlyList<TypeBundleCard> Items { get; private set; } = [];
    public int TotalItems { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var rows = await bundles.GetAll(cancellationToken);
        TotalItems = rows.Count;
        Search = Normalize(Search);
        Items = rows
            .Select(x => new TypeBundleCard(
                x.Id,
                x.AssemblyId,
                x.AssemblyKey,
                x.AssemblyDescription,
                x.AssemblyVersion,
                x.LanguageKey,
                x.IsActive,
                CountTypes(x),
                x.UpdatedAtUtc))
            .Where(x => string.IsNullOrWhiteSpace(Search) || MatchesSearch(x))
            .OrderBy(x => x.LanguageKey)
            .ThenBy(x => x.AssemblyKey)
            .ToArray();
    }

    public async Task<IActionResult> OnPostSaveBundleAsync([FromBody] ButterMorphLanguageTypeBundle bundle, CancellationToken cancellationToken)
    {
        if (bundle is null)
        {
            return new JsonResult(new { succeeded = false, message = "Type bundle is required." });
        }

        var entity = KnOwlLanguageTypeMappingHost.ToEntity(bundle);
        await bundles.Upsert(entity, cancellationToken);
        return new JsonResult(new { succeeded = true, message = "Type bundle saved." });
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await bundles.SetActive(id, isActive: false, DateTime.UtcNow, cancellationToken);
        return RedirectToPage();
    }

    private bool MatchesSearch(TypeBundleCard item)
        => Contains(item.AssemblyId)
            || Contains(item.AssemblyKey)
            || Contains(item.Description)
            || Contains(item.AssemblyVersion)
            || Contains(item.LanguageKey)
            || Contains(item.IsActive ? "Active" : "Inactive");

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static int CountTypes(KnOwl.ControlPlane.Design.Core.LanguageTypeBundleDefinition bundle)
        => KnOwlLanguageTypeMappingHost.ReadBundle(bundle)?.Types.Count ?? 0;

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record TypeBundleCard(
        Guid Id,
        string AssemblyId,
        string AssemblyKey,
        string? Description,
        string AssemblyVersion,
        string LanguageKey,
        bool IsActive,
        int TypeCount,
        DateTime UpdatedAtUtc);
}

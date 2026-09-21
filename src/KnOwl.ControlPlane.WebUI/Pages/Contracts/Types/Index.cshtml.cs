using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Types;

public class IndexModel(ISchemaTypeInteractionService schemaTypes) : PageModel
{
    public IReadOnlyList<SchemaTypeDefinition> Types { get; private set; } = [];
    public int TotalTypes { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var typeRows = await schemaTypes.GetAll(cancellationToken);
        TotalTypes = typeRows.Count;
        Search = Normalize(Search);
        Types = (string.IsNullOrWhiteSpace(Search)
                ? typeRows
                : typeRows.Where(MatchesSearch))
            .OrderBy(x => x.IsSystem)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await schemaTypes.GetById(id, cancellationToken: cancellationToken);
        if (entity is not null)
        {
            await schemaTypes.UpdateDefinition(
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

    private bool MatchesSearch(SchemaTypeDefinition item)
        => Contains(item.Name)
            || Contains(item.Key)
            || Contains(item.Description)
            || Contains(item.IsSystem ? "System" : "Custom")
            || Contains(item.IsActive ? "Active" : "Inactive")
            || item.Versions.Any(version =>
                Contains(version.VersionNumber)
                || Contains(version.Comment)
                || Contains(version.DefinitionJson)
                || Contains(version.IsActive ? "Active" : "Inactive"));

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

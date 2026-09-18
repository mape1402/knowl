using System.ComponentModel.DataAnnotations;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class IndexModel(IDocumentationInteractionService documentation) : PageModel
{
    public IReadOnlyList<KnOwl.Documentation.DocumentationSpace> Spaces { get; private set; } = [];
    public string? Search { get; private set; }
    public bool ShowNewSpaceModal { get; private set; }

    [BindProperty]
    public SpaceInput NewSpace { get; set; } = new();

    public async Task OnGetAsync(string? search, CancellationToken cancellationToken)
    {
        await Load(search, cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateSpaceAsync(string? search, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ShowNewSpaceModal = true;
            await Load(search, cancellationToken);
            return Page();
        }

        var space = await documentation.UpsertSpace(NewSpace.Key, NewSpace.Name, NewSpace.Description, isActive: true, cancellationToken);
        return RedirectToPage("/Documentation/Space", new { id = space.Id });
    }

    private async Task Load(string? search, CancellationToken cancellationToken)
    {
        Search = search;
        var spaces = await documentation.GetSpaces(cancellationToken);
        Spaces = string.IsNullOrWhiteSpace(search)
            ? spaces
            : spaces.Where(space =>
                space.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                space.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (space.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();
    }

    public sealed class SpaceInput
    {
        [Required]
        [Display(Name = "Key")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}

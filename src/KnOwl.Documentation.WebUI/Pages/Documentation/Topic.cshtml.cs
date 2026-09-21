using System.ComponentModel.DataAnnotations;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class TopicModel(IDocumentationInteractionService documentation) : PageModel
{
    public KnOwl.Documentation.DocumentationSpace? Space { get; private set; }
    public KnOwl.Documentation.DocumentationTopic? Topic { get; private set; }
    public IReadOnlyList<KnOwl.Documentation.DocumentationPage> Pages { get; private set; } = [];
    public string? Search { get; private set; }
    public bool ShowNewPageModal { get; private set; }

    [BindProperty]
    public DocumentationPageInput NewPage { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        if (!await Load(id, search, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreatePageAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        if (!await Load(id, search, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ShowNewPageModal = true;
            return Page();
        }

        var page = await documentation.UpsertPage(Space!.Key, Topic!.Key, NewPage.Key, NewPage.Title, NewPage.Description, isActive: true, cancellationToken);
        return RedirectToPage("/Documentation/Page", new { id = page.Id });
    }

    private async Task<bool> Load(Guid id, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        Topic = await documentation.GetTopic(id, cancellationToken);
        if (Topic is null)
        {
            return false;
        }

        var spaces = await documentation.GetSpaces(cancellationToken);
        Space = spaces.FirstOrDefault(x => x.Id == Topic.SpaceId);
        if (Space is null)
        {
            return false;
        }

        var pages = await documentation.GetPages(Space.Key, Topic.Key, cancellationToken);
        Pages = string.IsNullOrWhiteSpace(search)
            ? pages
            : pages.Where(page =>
                page.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                page.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (page.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (page.IsActive ? "Active" : "Inactive").Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return true;
    }

    public sealed class DocumentationPageInput
    {
        [Required]
        [Display(Name = "Key")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}

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
    public IReadOnlyList<DocumentationPageCard> PageCards { get; private set; } = [];
    public string? Search { get; private set; }
    public bool ShowNewPageModal { get; private set; }

    [BindProperty]
    public DocumentationPageInput NewPage { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid spaceId, Guid topicId, string? search, CancellationToken cancellationToken)
    {
        if (!await Load(spaceId, topicId, search, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreatePageAsync(Guid spaceId, Guid topicId, string? search, CancellationToken cancellationToken)
    {
        if (!await Load(spaceId, topicId, search, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ShowNewPageModal = true;
            return Page();
        }

        var page = await documentation.UpsertPage(Space!.Key, Topic!.Key, NewPage.Key, NewPage.Title, NewPage.Description, isActive: true, cancellationToken);
        return RedirectToPage("/Documentation/Page", new { spaceId = Space.Id, topicId = Topic.Id, pageId = page.Id });
    }

    private async Task<bool> Load(Guid spaceId, Guid topicId, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        Space = await documentation.GetSpace(spaceId, cancellationToken);
        Topic = await documentation.GetTopic(topicId, cancellationToken);
        if (Space is null || Topic is null || Topic.SpaceId != Space.Id)
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
        PageCards = await BuildPageCards(Pages, cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<DocumentationPageCard>> BuildPageCards(
        IReadOnlyList<KnOwl.Documentation.DocumentationPage> pages,
        CancellationToken cancellationToken)
    {
        List<DocumentationPageCard> cards = [];
        foreach (var page in pages)
        {
            var pageWithVersions = await documentation.GetPage(page.Id, includeVersions: true, cancellationToken);
            var latestVersionId = LatestBrowsableVersion(pageWithVersions)?.Id;
            cards.Add(new DocumentationPageCard(page, latestVersionId));
        }

        return cards;
    }

    private static KnOwl.Documentation.DocumentationPageVersion? LatestBrowsableVersion(KnOwl.Documentation.DocumentationPage? page)
        => page?.Versions
            .Where(version => version.Status == KnOwl.Documentation.DocPageVersionStatus.Published)
            .OrderByDescending(version => version.PublishedAtUtc ?? version.CreatedAtUtc)
            .FirstOrDefault()
            ?? page?.Versions
                .OrderByDescending(version => version.PublishedAtUtc ?? version.CreatedAtUtc)
                .FirstOrDefault();

    public sealed record DocumentationPageCard(KnOwl.Documentation.DocumentationPage Page, Guid? LatestVersionId);

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

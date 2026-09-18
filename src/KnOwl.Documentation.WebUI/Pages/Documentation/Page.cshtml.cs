using System.ComponentModel.DataAnnotations;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class DocumentationPageModel(IDocumentationInteractionService documentation) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public KnOwl.Documentation.DocumentationSpace? Space { get; private set; }
    public KnOwl.Documentation.DocumentationTopic? Topic { get; private set; }
    public KnOwl.Documentation.DocumentationPage? PageItem { get; private set; }
    public IReadOnlyList<KnOwl.Documentation.DocumentationPageVersion> Versions { get; private set; } = [];
    public string? Search { get; private set; }
    public bool ShowNewVersionModal { get; private set; }

    [BindProperty]
    public VersionInput NewVersion { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        if (!await Load(id, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUploadVersionAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        if (!await Load(id, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid || NewVersion.File is null)
        {
            ShowNewVersionModal = true;
            return Page();
        }

        await using var stream = NewVersion.File.OpenReadStream();
        await documentation.ImportVersion(new DocumentationVersionInput(PageItem!.Id, NewVersion.VersionNumber, NewVersion.File.FileName, NewVersion.File.ContentType, stream, NewVersion.EntryPath), cancellationToken);
        return RedirectToPage("/Documentation/Page", new { id = PageItem.Id });
    }

    private async Task<bool> Load(Guid id, CancellationToken cancellationToken)
    {
        PageItem = await documentation.GetPage(id, includeVersions: true, cancellationToken);
        if (PageItem is null)
        {
            return false;
        }

        var topics = (await documentation.GetSpaces(cancellationToken))
            .SelectMany(space => space.Topics.Select(topic => (space, topic)));
        foreach (var (space, topic) in topics)
        {
            if (topic.Id == PageItem.TopicId)
            {
                Space = space;
                Topic = topic;
                break;
            }
        }

        if (Topic is null)
        {
            foreach (var space in await documentation.GetSpaces(cancellationToken))
            {
                var matchingTopics = await documentation.GetTopics(space.Key, cancellationToken);
                Topic = matchingTopics.FirstOrDefault(x => x.Id == PageItem.TopicId);
                if (Topic is not null)
                {
                    Space = space;
                    break;
                }
            }
        }

        var versions = PageItem.Versions.OrderByDescending(x => x.VersionNumber);
        Versions = string.IsNullOrWhiteSpace(Search)
            ? versions.ToArray()
            : versions.Where(version =>
                version.VersionNumber.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                version.EntryPath.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                version.ContentHash.Contains(Search, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return Space is not null && Topic is not null;
    }

    public sealed class VersionInput
    {
        [Required]
        [Display(Name = "Version")]
        public string VersionNumber { get; set; } = string.Empty;

        [Display(Name = "Entry path")]
        public string? EntryPath { get; set; }

        [Required]
        [Display(Name = "Markdown or ZIP")]
        public IFormFile? File { get; set; }
    }
}

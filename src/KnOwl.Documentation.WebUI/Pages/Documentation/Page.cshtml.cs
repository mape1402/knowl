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

    public async Task<IActionResult> OnGetAsync(Guid spaceId, Guid topicId, Guid pageId, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        if (!await Load(spaceId, topicId, pageId, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUploadVersionAsync(Guid spaceId, Guid topicId, Guid pageId, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        if (!await Load(spaceId, topicId, pageId, cancellationToken))
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
        return RedirectToPage("/Documentation/Page", new { spaceId = Space!.Id, topicId = Topic!.Id, pageId = PageItem.Id });
    }

    private async Task<bool> Load(Guid spaceId, Guid topicId, Guid pageId, CancellationToken cancellationToken)
    {
        Space = await documentation.GetSpace(spaceId, cancellationToken);
        Topic = await documentation.GetTopic(topicId, cancellationToken);
        PageItem = await documentation.GetPage(pageId, includeVersions: true, cancellationToken);
        if (Space is null || Topic is null || PageItem is null || Topic.SpaceId != Space.Id || PageItem.TopicId != Topic.Id)
        {
            return false;
        }

        var versions = PageItem.Versions.OrderByDescending(x => x.VersionNumber);
        Versions = string.IsNullOrWhiteSpace(Search)
            ? versions.ToArray()
            : versions.Where(version =>
                version.VersionNumber.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                version.EntryPath.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                version.ContentHash.Contains(Search, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return true;
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

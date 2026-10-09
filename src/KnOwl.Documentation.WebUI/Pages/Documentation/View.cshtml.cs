using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class ViewModel(IDocumentationInteractionService documentation) : PageModel
{
    public Guid SpaceId { get; private set; }
    public Guid TopicId { get; private set; }
    public Guid PageId { get; private set; }
    public Guid VersionId { get; private set; }
    public string SpaceKey { get; private set; } = string.Empty;
    public string TopicKey { get; private set; } = string.Empty;
    public string PageKey { get; private set; } = string.Empty;
    public string PageTitle { get; private set; } = "Documentation";
    public RenderedDocumentation? Rendered { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid spaceId, Guid topicId, Guid pageId, Guid versionId, CancellationToken cancellationToken)
    {
        SpaceId = spaceId;
        TopicId = topicId;
        PageId = pageId;
        VersionId = versionId;

        Rendered = await documentation.Render(versionId, cancellationToken);
        var hierarchy = await DocumentationRouteValidator.LoadHierarchy(documentation, spaceId, topicId, pageId, cancellationToken);
        if (Rendered is null || hierarchy is null || Rendered.Version.PageId != pageId)
        {
            return NotFound();
        }

        SpaceKey = hierarchy.Space.Key;
        TopicKey = hierarchy.Topic.Key;
        PageKey = hierarchy.Page.Key;
        PageTitle = $"{hierarchy.Page.Title} {Rendered.Version.VersionNumber}";
        return Page();
    }
}

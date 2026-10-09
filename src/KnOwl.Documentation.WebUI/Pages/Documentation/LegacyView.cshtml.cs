using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class LegacyViewModel(IDocumentationInteractionService documentation) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string spaceKey, string topicKey, string pageKey, string? version, CancellationToken cancellationToken)
    {
        var rendered = await documentation.Render(spaceKey, topicKey, pageKey, version, cancellationToken);
        if (rendered is null)
        {
            return NotFound();
        }

        var page = await documentation.GetPage(rendered.Version.PageId, cancellationToken: cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        var topic = await documentation.GetTopic(page.TopicId, cancellationToken);
        return topic is null
            ? NotFound()
            : RedirectToPage("/Documentation/View", new { spaceId = topic.SpaceId, topicId = topic.Id, pageId = page.Id, versionId = rendered.Version.Id });
    }
}

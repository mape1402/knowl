using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class LegacyPageModel(IDocumentationInteractionService documentation) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var page = await documentation.GetPage(id, cancellationToken: cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        var topic = await documentation.GetTopic(page.TopicId, cancellationToken);
        return topic is null
            ? NotFound()
            : RedirectToPage("/Documentation/Page", new { spaceId = topic.SpaceId, topicId = topic.Id, pageId = page.Id });
    }
}

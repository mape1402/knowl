using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class LegacyTopicModel(IDocumentationInteractionService documentation) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var topic = await documentation.GetTopic(id, cancellationToken);
        return topic is null
            ? NotFound()
            : RedirectToPage("/Documentation/Topic", new { spaceId = topic.SpaceId, topicId = topic.Id });
    }
}

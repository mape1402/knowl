using System.ComponentModel.DataAnnotations;
using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class SpaceModel(IDocumentationInteractionService documentation) : PageModel
{
    public KnOwl.Documentation.DocumentationSpace? Space { get; private set; }
    public IReadOnlyList<KnOwl.Documentation.DocumentationTopic> Topics { get; private set; } = [];
    public string? Search { get; private set; }
    public bool ShowNewTopicModal { get; private set; }

    [BindProperty]
    public TopicInput NewTopic { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        return await Load(id, search, cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCreateTopicAsync(Guid id, string? search, CancellationToken cancellationToken)
    {
        if (!await Load(id, search, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ShowNewTopicModal = true;
            return Page();
        }

        var topic = await documentation.UpsertTopic(Space!.Key, NewTopic.Key, NewTopic.Name, NewTopic.Description, isActive: true, cancellationToken);
        return RedirectToPage("/Documentation/Topic", new { id = topic.Id });
    }

    private async Task<bool> Load(Guid id, string? search, CancellationToken cancellationToken)
    {
        Search = search;
        Space = await documentation.GetSpace(id, cancellationToken);
        if (Space is null)
        {
            return false;
        }

        var topics = await documentation.GetTopics(Space.Key, cancellationToken);
        Topics = string.IsNullOrWhiteSpace(search)
            ? topics
            : topics.Where(topic =>
                topic.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                topic.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (topic.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();
        return true;
    }

    public sealed class TopicInput
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

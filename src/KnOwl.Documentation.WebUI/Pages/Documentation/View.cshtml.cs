using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class ViewModel(IDocumentationInteractionService documentation) : PageModel
{
    public string SpaceKey { get; private set; } = string.Empty;
    public string TopicKey { get; private set; } = string.Empty;
    public string PageKey { get; private set; } = string.Empty;
    public string PageTitle { get; private set; } = "Documentation";
    public RenderedDocumentation? Rendered { get; private set; }

    public async Task OnGetAsync(string spaceKey, string topicKey, string pageKey, string? version, CancellationToken cancellationToken)
    {
        SpaceKey = spaceKey;
        TopicKey = topicKey;
        PageKey = pageKey;
        Rendered = await documentation.Render(spaceKey, topicKey, pageKey, version, cancellationToken);
        PageTitle = Rendered is null ? pageKey : $"{pageKey} {Rendered.Version.VersionNumber}";
    }
}

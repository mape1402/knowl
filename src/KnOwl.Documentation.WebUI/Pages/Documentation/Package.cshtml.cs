using KnOwl.Documentation.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class PackageModel(IDocumentationInteractionService documentation) : PageModel
{
    public async Task<IActionResult> OnGetAsync(Guid spaceId, Guid topicId, Guid pageId, Guid versionId, CancellationToken cancellationToken)
    {
        var rendered = await documentation.Render(versionId, cancellationToken);
        var hierarchy = await DocumentationRouteValidator.LoadHierarchy(documentation, spaceId, topicId, pageId, cancellationToken);
        if (rendered is null || hierarchy is null || rendered.Version.PageId != pageId)
        {
            return NotFound();
        }

        var package = await documentation.BuildSourcePackage(versionId, cancellationToken);
        return File(package.Content, "application/zip", package.FileName);
    }
}

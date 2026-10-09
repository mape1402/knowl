using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class LegacySpaceModel : PageModel
{
    public IActionResult OnGet(Guid id)
        => RedirectToPage("/Documentation/Space", new { spaceId = id });
}

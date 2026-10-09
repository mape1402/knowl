using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Documentation.WebUI.Pages.Documentation;

public sealed class LegacyIndexModel : PageModel
{
    public IActionResult OnGet()
        => RedirectToPage("/Documentation/Index");
}

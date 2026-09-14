using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts;

public class SchemasModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Contracts/Events/Index");
    }
}

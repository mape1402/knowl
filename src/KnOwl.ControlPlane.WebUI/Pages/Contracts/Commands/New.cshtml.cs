using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class NewModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Contracts/Commands/Index");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Contracts/Commands/Index");
    }
}

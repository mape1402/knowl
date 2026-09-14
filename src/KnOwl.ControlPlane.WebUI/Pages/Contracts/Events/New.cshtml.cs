using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Events;

public class NewModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Contracts/Events/Index");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Contracts/Events/Index");
    }
}

using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Events;

public class IndexModel(IEventInteractionService events) : PageModel
{
    public IReadOnlyList<EventDefinition> Events { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await events.GetAll(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await events.Delete(id, cancellationToken);
        return RedirectToPage();
    }
}

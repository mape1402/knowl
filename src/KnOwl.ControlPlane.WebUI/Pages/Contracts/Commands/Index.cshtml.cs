using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class IndexModel(ICommandInteractionService commands) : PageModel
{
    public IReadOnlyList<CommandDefinition> Commands { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Commands = await commands.GetAll(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await commands.Delete(id, cancellationToken);
        return RedirectToPage();
    }
}

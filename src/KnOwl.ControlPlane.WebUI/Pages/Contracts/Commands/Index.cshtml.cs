using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Commands;

public class IndexModel(ICommandInteractionService commands) : PageModel
{
    public IReadOnlyList<CommandDefinition> Commands { get; private set; } = [];
    public int TotalCommands { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var commandRows = await commands.GetAll(cancellationToken);
        TotalCommands = commandRows.Count;
        Search = Normalize(Search);
        Commands = string.IsNullOrWhiteSpace(Search)
            ? commandRows
            : commandRows.Where(MatchesSearch).ToArray();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await commands.Delete(id, cancellationToken);
        return RedirectToPage();
    }

    private bool MatchesSearch(CommandDefinition item)
        => Contains(item.Name)
            || Contains(item.Topic)
            || Contains(item.Description)
            || Contains(item.IsActive ? "Active" : "Inactive")
            || item.Versions.Any(version =>
                Contains(version.VersionNumber)
                || Contains(version.Comment)
                || Contains(version.Status.ToString()));

    private bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(Search)
            && !string.IsNullOrWhiteSpace(value)
            && value.Contains(Search, StringComparison.OrdinalIgnoreCase);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

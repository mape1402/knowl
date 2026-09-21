using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Design.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Contracts.Events;

public class IndexModel(IEventInteractionService events) : PageModel
{
    public IReadOnlyList<EventDefinition> Events { get; private set; } = [];
    public int TotalEvents { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var eventRows = await events.GetAll(cancellationToken);
        TotalEvents = eventRows.Count;
        Search = Normalize(Search);
        Events = string.IsNullOrWhiteSpace(Search)
            ? eventRows
            : eventRows.Where(MatchesSearch).ToArray();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await events.Delete(id, cancellationToken);
        return RedirectToPage();
    }

    private bool MatchesSearch(EventDefinition item)
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

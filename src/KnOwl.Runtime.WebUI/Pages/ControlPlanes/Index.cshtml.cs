using KnOwl.Runtime.Distribution;
using KnOwl.Runtime.Storage;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages.ControlPlanes;

/// <summary>
/// Lists control-plane connections known by the runtime host.
/// </summary>
public sealed class IndexModel(IRuntimeDesignNodeRepository designNodes) : PageModel
{
    /// <summary>
    /// Gets the configured control-plane design nodes.
    /// </summary>
    public IReadOnlyList<RuntimeDesignNode> DesignNodes { get; private set; } = [];

    /// <summary>
    /// Loads runtime control-plane connections.
    /// </summary>
    public async Task OnGet(CancellationToken cancellationToken)
    {
        DesignNodes = await designNodes.GetAll(cancellationToken);
    }
}

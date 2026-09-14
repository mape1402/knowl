using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.Runtime.WebUI.Pages;

/// <summary>
/// Shows the fallback error page for the runtime Web UI.
/// </summary>
public sealed class ErrorModel : PageModel
{
    /// <summary>
    /// Gets the request identifier attached to the failed request.
    /// </summary>
    public string RequestId { get; private set; } = string.Empty;

    /// <summary>
    /// Handles rendering of the error page.
    /// </summary>
    public void OnGet()
    {
        RequestId = HttpContext.TraceIdentifier;
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Apis;

public sealed class IndexModel : PageModel
{
    public UnderConstructionViewModel Placeholder { get; } = new(
        "API Overview",
        "This workspace will summarize REST and gRPC contracts exposed by the platform.",
        "journal-code",
        [
            "Catalog health and availability by API surface.",
            "Entry points for REST endpoints and gRPC services.",
            "Cross-links from API routes to the async contracts they depend on."
        ],
        "Back to Home",
        "/");
}

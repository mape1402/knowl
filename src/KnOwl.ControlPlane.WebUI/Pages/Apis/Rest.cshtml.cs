using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Apis;

public sealed class RestModel : PageModel
{
    public UnderConstructionViewModel Placeholder { get; } = new(
        "REST Catalog",
        "The REST catalog will centralize endpoint metadata, versions, and documentation links.",
        "braces",
        [
            "Endpoint inventory grouped by service and resource.",
            "OpenAPI metadata, version history, and publish status.",
            "Links from REST operations to request and response schemas."
        ],
        "Back to API Overview",
        "/apis");
}

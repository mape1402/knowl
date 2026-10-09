using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Apis;

public sealed class GrpcModel : PageModel
{
    public UnderConstructionViewModel Placeholder { get; } = new(
        "gRPC Services",
        "This view will expose service contracts, protobuf metadata, and runtime readiness.",
        "hdd-network",
        [
            "Service and method inventory by package.",
            "Proto file metadata, schema mapping, and compatibility notes.",
            "Runtime availability and release status for service contracts."
        ],
        "Back to API Overview",
        "/apis");
}

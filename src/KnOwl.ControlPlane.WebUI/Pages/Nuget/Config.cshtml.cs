using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Nuget;

public sealed class ConfigModel : PageModel
{
    public UnderConstructionViewModel Placeholder { get; } = new(
        "Feed Config",
        "Feed configuration will manage publishing targets, credentials, and package retention policy.",
        "gear",
        [
            "Feed endpoints and publishing credentials.",
            "Package retention and promotion rules.",
            "Validation checks before generated packages are published."
        ],
        "Back to Packages",
        "/nuget/packages");
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KnOwl.ControlPlane.WebUI.Pages.Nuget;

public sealed class PackagesModel : PageModel
{
    public UnderConstructionViewModel Placeholder { get; } = new(
        "NuGet Packages",
        "Package browsing will show generated client and contract packages for downstream consumers.",
        "archive",
        [
            "Package list by feed, version, status, and target framework.",
            "Release notes, dependency metadata, and download links.",
            "Traceability from package versions back to KnOwl contracts."
        ],
        "Back to Home",
        "/");
}

using KnOwl.ControlPlane.WebUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace KnOwl.ControlPlane.WebUI.Pages
{
    public class IndexModel(
        IOptions<KnOwlControlPlaneHomeOptions> homeOptions) : PageModel
    {
        public KnOwlControlPlaneHomeOptions Home => homeOptions.Value;

        public IActionResult OnGet()
        {
            if (Home.RedirectRootToPath &&
                TryGetCustomHomePath(Home.Path, out var customHomePath) &&
                !Request.Path.StartsWithSegments(customHomePath, StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(customHomePath.Value!);
            }

            return Page();
        }

        private static bool TryGetCustomHomePath(string? path, out PathString customHomePath)
        {
            customHomePath = PathString.Empty;
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("/", StringComparison.Ordinal) ||
                string.Equals(path, "/", StringComparison.Ordinal))
            {
                return false;
            }

            customHomePath = new PathString(path);
            return true;
        }
    }
}

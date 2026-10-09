using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace KnOwl.ControlPlaneHost.Sample.Pages.Company;

[AllowAnonymous]
public sealed class LoginModel : PageModel
{
    public string ChallengeUrl { get; private set; } = "/auth/login/challenge";

    public void OnGet()
    {
        var returnUrl = Request.Query["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(returnUrl, UriKind.Relative, out _) ||
            returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            returnUrl = "/";
        }

        ChallengeUrl = QueryHelpers.AddQueryString("/auth/login/challenge", "returnUrl", returnUrl);
    }
}

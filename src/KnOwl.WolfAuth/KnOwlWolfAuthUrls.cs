using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace KnOwl.WolfAuth;

internal static class KnOwlWolfAuthUrls
{
    public static string BuildChallengeUrl(HttpContext context, KnOwlWolfAuthOptions options)
    {
        var returnUrl = NormalizeLocalReturnUrl(context.Request.Query[options.ReturnUrlParameter].ToString());
        return QueryHelpers.AddQueryString(options.ChallengePath, options.ReturnUrlParameter, returnUrl);
    }

    public static string NormalizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(returnUrl, UriKind.Relative, out var uri) ||
            returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return uri.ToString();
    }
}

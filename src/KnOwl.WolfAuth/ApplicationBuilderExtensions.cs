using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnOwl.WolfAuth;

/// <summary>
/// Maps the KnOwl login shell and authentication challenge endpoints.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Redirects anonymous browser requests to the KnOwl login shell when WolfAuth integration is enabled.
    /// </summary>
    public static IApplicationBuilder UseKnOwlWolfAuthLoginGate(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.ApplicationServices.GetService<IKnOwlWolfAuthRegistration>() is null)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            if (ShouldShowLogin(context, app.ApplicationServices.GetRequiredService<IOptions<KnOwlWolfAuthOptions>>().Value))
            {
                var options = app.ApplicationServices.GetRequiredService<IOptions<KnOwlWolfAuthOptions>>().Value;
                var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                context.Response.Redirect(QueryHelpers.AddQueryString(options.LoginPath, options.ReturnUrlParameter, returnUrl));
                return;
            }

            await next(context);
        });
    }

    /// <summary>
    /// Maps the login page, challenge endpoint, and logout endpoint when WolfAuth integration is enabled.
    /// </summary>
    public static IEndpointRouteBuilder MapKnOwlWolfAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (endpoints.ServiceProvider.GetService<IKnOwlWolfAuthRegistration>() is null)
        {
            return endpoints;
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<KnOwlWolfAuthOptions>>().Value;

        endpoints.MapGet(
                options.LoginPath,
                (HttpContext context, IOptions<KnOwlWolfAuthOptions> options) =>
                    Results.Content(RenderLoginPage(context, options.Value), "text/html"))
            .AllowAnonymous()
            .WithName("KnOwlWolfAuthLogin");

        endpoints.MapGet(
                options.ChallengePath,
                (HttpContext context, IOptions<KnOwlWolfAuthOptions> options) =>
                    Challenge(context, options.Value))
            .AllowAnonymous()
            .WithName("KnOwlWolfAuthChallenge");

        endpoints.MapGet(
                options.LogoutPath,
                (HttpContext context, IOptions<KnOwlWolfAuthOptions> options) =>
                    SignOut(context, options.Value))
            .WithName("KnOwlWolfAuthLogout");

        return endpoints;
    }

    private static bool ShouldShowLogin(HttpContext context, KnOwlWolfAuthOptions options)
    {
        if (context.User.Identity?.IsAuthenticated == true ||
            !HttpMethods.IsGet(context.Request.Method) ||
            IsBypassPath(context.Request.Path, options))
        {
            return false;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return string.IsNullOrWhiteSpace(accept) ||
            accept.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
            accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBypassPath(PathString path, KnOwlWolfAuthOptions options)
    {
        return path.StartsWithSegments(options.LoginPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments(options.ChallengePath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments(options.LogoutPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
            path.Value?.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static IResult Challenge(HttpContext context, KnOwlWolfAuthOptions options)
    {
        var returnUrl = NormalizeLocalReturnUrl(
            context.Request.Query[options.ReturnUrlParameter].ToString());
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };

        return options.ChallengeSchemes.Count == 0
            ? Results.Challenge(properties)
            : Results.Challenge(properties, options.ChallengeSchemes);
    }

    private static IResult SignOut(HttpContext context, KnOwlWolfAuthOptions options)
    {
        var returnUrl = NormalizeLocalReturnUrl(
            context.Request.Query[options.ReturnUrlParameter].ToString());
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };

        return options.SignOutSchemes.Count == 0
            ? Results.SignOut(properties)
            : Results.SignOut(properties, options.SignOutSchemes);
    }

    private static string NormalizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(returnUrl, UriKind.Relative, out var uri) ||
            returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return uri.ToString();
    }

    private static string RenderLoginPage(HttpContext context, KnOwlWolfAuthOptions options)
    {
        var returnUrl = NormalizeLocalReturnUrl(context.Request.Query[options.ReturnUrlParameter].ToString());
        var challengeUrl = QueryHelpers.AddQueryString(options.ChallengePath, options.ReturnUrlParameter, returnUrl);
        var title = WebUtility.HtmlEncode(options.ApplicationName);
        var subtitle = WebUtility.HtmlEncode(options.Subtitle);
        var buttonText = WebUtility.HtmlEncode(options.LoginButtonText);
        return $$$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{{title}}} - Login</title>
    <style>
        :root{color-scheme:light dark;--bg:#f4f6fb;--panel:#fff;--text:#07051d;--muted:#536079;--border:#d8ddec;--primary:#2563eb;--primary-hover:#1d4ed8;--shadow:0 24px 70px rgba(15,23,42,.16)}
        @media (prefers-color-scheme: dark){:root{--bg:#0f172a;--panel:#1e293b;--text:#f8fafc;--muted:#bfdbfe;--border:#334155;--primary:#60a5fa;--primary-hover:#93c5fd;--shadow:0 24px 80px rgba(0,0,0,.34)}}
        *{box-sizing:border-box}
        body{min-height:100vh;margin:0;display:grid;place-items:center;background:var(--bg);color:var(--text);font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}
        main{width:min(420px,calc(100vw - 32px));background:var(--panel);border:1px solid var(--border);border-radius:8px;box-shadow:var(--shadow);padding:28px}
        h1{margin:0;font-size:1.45rem;line-height:1.2}
        p{margin:10px 0 24px;color:var(--muted)}
        a{display:inline-flex;align-items:center;justify-content:center;width:100%;min-height:44px;border-radius:7px;background:var(--primary);color:#fff;text-decoration:none;font-weight:700}
        a:hover{background:var(--primary-hover)}
    </style>
</head>
<body>
    <main>
        <h1>{{{title}}}</h1>
        <p>{{{subtitle}}}</p>
        <a href="{{{WebUtility.HtmlEncode(challengeUrl)}}}">{{{buttonText}}}</a>
    </main>
</body>
</html>
""";
    }
}

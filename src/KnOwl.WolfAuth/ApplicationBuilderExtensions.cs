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

        if (options.MapLoginEndpoint)
        {
            endpoints.MapGet(
                    options.LoginPath,
                    (HttpContext context, IOptions<KnOwlWolfAuthOptions> options, IKnOwlLoginPageRenderer renderer) =>
                        renderer.Render(context, options.Value))
                .AllowAnonymous()
                .WithName("KnOwlWolfAuthLogin");
        }

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
        var returnUrl = KnOwlWolfAuthUrls.NormalizeLocalReturnUrl(
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
        var returnUrl = KnOwlWolfAuthUrls.NormalizeLocalReturnUrl(
            context.Request.Query[options.ReturnUrlParameter].ToString());
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl
        };

        return options.SignOutSchemes.Count == 0
            ? Results.SignOut(properties)
            : Results.SignOut(properties, options.SignOutSchemes);
    }

}

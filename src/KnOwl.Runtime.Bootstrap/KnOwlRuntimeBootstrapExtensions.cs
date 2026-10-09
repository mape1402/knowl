using KnOwl.Runtime.Api;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Bootstrap.Grpc;
using KnOwl.Runtime.Bootstrap.Runtime;
using KnOwl.Runtime.Storage.EntityFramework;
using KnOwl.Runtime.WebUI;
using KnOwl.WolfAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WolfAuth.AspNetCore;

namespace KnOwl.Runtime.Bootstrap;

/// <summary>
/// Registers and maps reusable KnOwl Runtime host behavior.
/// </summary>
public static class KnOwlRuntimeBootstrapExtensions
{
    /// <summary>
    /// Adds the complete KnOwl Runtime composition to an ASP.NET Core host.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KnOwlRuntimeBootstrapOptions>? configure = null)
    {
        var options = new KnOwlRuntimeBootstrapOptions();
        configure?.Invoke(options);

        services.AddRazorPages();
        services.AddKnOwlRuntimeWebUI(theme =>
        {
            theme.Title = options.Theme.Title;
            theme.Mode = options.Theme.Mode;
            theme.Subtitle = options.Theme.Subtitle;
            theme.IconCssClass = options.Theme.IconCssClass;
            theme.IconImageUrl = options.Theme.IconImageUrl;
            CopyThemePalette(options.Theme.Light, theme.Light);
            CopyThemePalette(options.Theme.Dark, theme.Dark);
        });
        services.AddHealthChecks();
        services.AddAuthorization();
        services.AddGrpc();
        services.Configure<KnOwlRuntimeArtifactPullOptions>(
            configuration.GetSection("Runtime:ArtifactPull"));
        services.AddHostedService<KnOwlRuntimeArtifactPullWorker>();

        services.AddKnOwlRuntimeStorageEntityFramework(options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Runtime"));
        services.AddKnOwlRuntimeApplication();

        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Maps the complete KnOwl Runtime HTTP and gRPC surface.
    /// </summary>
    public static WebApplication MapKnOwlRuntime(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();
        if (app.Services.GetService<IAuthenticationSchemeProvider>() is not null)
        {
            app.UseAuthentication();
        }

        if (app.Services.GetService<IKnOwlWolfAuthRegistration>() is not null)
        {
            app.UseWolfAuth();
        }

        app.UseKnOwlWolfAuthLoginGate();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapKnOwlWolfAuthEndpoints();
        var options = app.Services.GetRequiredService<KnOwlRuntimeBootstrapOptions>();
        app.MapKnOwlRuntimeApi(options.Authorization);
        app.MapKnOwlRuntimeEndpoints();
        app.MapGrpcService<RuntimeContractsGrpcService>();
        app.MapGet("/runtime/status", () => Results.Ok(new { service = "KnOwl.Runtime", status = "ok" }));
        app.MapRazorPages();

        return app;
    }

    private static void CopyThemePalette(
        KnOwlRuntimeThemePaletteOptions source,
        KnOwlRuntimeThemePaletteOptions target)
    {
        target.PrimaryColor = source.PrimaryColor;
        target.PrimaryHoverColor = source.PrimaryHoverColor;
        target.SidebarBackgroundColor = source.SidebarBackgroundColor;
        target.SidebarBrandBackgroundColor = source.SidebarBrandBackgroundColor;
        target.SidebarTextColor = source.SidebarTextColor;
        target.SidebarMutedTextColor = source.SidebarMutedTextColor;
        target.ContentBackgroundColor = source.ContentBackgroundColor;
        target.SurfaceColor = source.SurfaceColor;
        target.TextColor = source.TextColor;
        target.MutedTextColor = source.MutedTextColor;
        target.BorderColor = source.BorderColor;
        target.SubtleBackgroundColor = source.SubtleBackgroundColor;
        target.CodeBackgroundColor = source.CodeBackgroundColor;
        target.CodeTextColor = source.CodeTextColor;
        target.ShadowColor = source.ShadowColor;
    }

    private static Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> ThrowMissingStorageConfiguration(string storageName)
        => _ => throw new InvalidOperationException(
            $"KnOwl {storageName} storage requires an EF Core provider configuration. Set ConfigureStorage in the bootstrap options.");
}

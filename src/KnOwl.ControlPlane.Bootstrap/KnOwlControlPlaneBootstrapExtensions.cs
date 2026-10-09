using ButterMorph.Web.Razor;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Bootstrap.Contracts;
using KnOwl.ControlPlane.Bootstrap.Distribution;
using KnOwl.ControlPlane.Storage.EntityFramework;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution;
using KnOwl.ControlPlane.WebUI;
using KnOwl.Documentation.Api;
using KnOwl.Documentation.Application;
using KnOwl.Documentation.Storage.EntityFramework;
using KnOwl.Documentation.WebUI;
using KnOwl.WolfAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WolfAuth.AspNetCore;

namespace KnOwl.ControlPlane.Bootstrap;

/// <summary>
/// Registers and maps reusable KnOwl Control Plane host behavior.
/// </summary>
public static class KnOwlControlPlaneBootstrapExtensions
{
    /// <summary>
    /// Adds the complete KnOwl Control Plane composition to an ASP.NET Core host.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlane(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KnOwlControlPlaneBootstrapOptions>? configure = null)
    {
        var options = new KnOwlControlPlaneBootstrapOptions();
        configure?.Invoke(options);

        services.AddRazorPages();
        services.AddKnOwlControlPlaneWebUI(theme =>
        {
            theme.Title = options.Theme.Title;
            theme.Mode = options.Theme.Mode;
            theme.IconCssClass = options.Theme.IconCssClass;
            theme.IconImageUrl = options.Theme.IconImageUrl;
            CopyThemePalette(options.Theme.Light, theme.Light);
            CopyThemePalette(options.Theme.Dark, theme.Dark);
        });
        services.AddKnOwlDocumentationWebUI();
        services.AddHealthChecks();
        services.AddAuthorization();
        services.AddKnOwlControlPlaneApplication();
        services.AddKnOwlControlPlaneDistributionApplication();
        services.AddKnOwlDocumentationApplication();

        services.AddKnOwlControlPlaneStorageEntityFramework(options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Control Plane"));
        services.AddKnOwlControlPlaneDistributionStorageEntityFramework();
        services.AddKnOwlDocumentationStorageEntityFramework(
            options.ConfigureDocumentationStorage ?? options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Control Plane documentation"));

        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Maps the complete KnOwl Control Plane HTTP surface.
    /// </summary>
    public static WebApplication MapKnOwlControlPlane(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
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

        var options = app.Services.GetRequiredService<KnOwlControlPlaneBootstrapOptions>();

        app.MapStaticAssets();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapKnOwlWolfAuthEndpoints();
        app.MapButterMorphDesigner(options.ButterMorphPath);
        app.MapKnOwlControlPlaneApi(options.Authorization);
        app.MapKnOwlControlPlaneContractCatalogEndpoints();
        app.MapKnOwlArtifactDeliveryEndpoints();
        app.MapKnOwlDocumentationApi(options.Authorization);
        app.MapRazorPages().WithStaticAssets();

        return app;
    }

    private static void CopyThemePalette(
        KnOwlControlPlaneThemePaletteOptions source,
        KnOwlControlPlaneThemePaletteOptions target)
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

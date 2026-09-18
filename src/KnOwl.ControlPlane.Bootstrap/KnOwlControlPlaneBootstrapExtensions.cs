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
using KnOwl.Security;
using KnOwl.Security.Storage.EntityFramework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            theme.IconCssClass = options.Theme.IconCssClass;
            theme.IconImageUrl = options.Theme.IconImageUrl;
            theme.PrimaryColor = options.Theme.PrimaryColor;
            theme.PrimaryHoverColor = options.Theme.PrimaryHoverColor;
            theme.SidebarBackgroundColor = options.Theme.SidebarBackgroundColor;
            theme.SidebarBrandBackgroundColor = options.Theme.SidebarBrandBackgroundColor;
            theme.SidebarTextColor = options.Theme.SidebarTextColor;
            theme.SidebarMutedTextColor = options.Theme.SidebarMutedTextColor;
            theme.ContentBackgroundColor = options.Theme.ContentBackgroundColor;
            theme.SurfaceColor = options.Theme.SurfaceColor;
            theme.TextColor = options.Theme.TextColor;
        });
        services.AddKnOwlDocumentationWebUI();
        services.AddHealthChecks();
        services.AddKnOwlSecurity(securityOptions =>
        {
            securityOptions.RequireKnownSubject = options.Security.RequireKnownSubject;
            securityOptions.AllowBootstrapAdminSync = options.Security.AllowBootstrapAdminSync;
            securityOptions.Subject.Provider = options.Security.Subject.Provider;
            CopyList(options.Security.Subject.SubjectIdClaimTypes, securityOptions.Subject.SubjectIdClaimTypes);
            CopyList(options.Security.Subject.DisplayNameClaimTypes, securityOptions.Subject.DisplayNameClaimTypes);
            CopyList(options.Security.Subject.EmailClaimTypes, securityOptions.Subject.EmailClaimTypes);
            CopyList(options.Security.Subject.GroupClaimTypes, securityOptions.Subject.GroupClaimTypes);
            securityOptions.BootstrapAdmins.Clear();
            securityOptions.BootstrapAdmins.AddRange(options.Security.BootstrapAdmins);
        });
        services.AddKnOwlControlPlaneApplication();
        services.AddKnOwlControlPlaneDistributionApplication();
        services.AddKnOwlDocumentationApplication();

        services.AddKnOwlControlPlaneStorageEntityFramework(options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Control Plane"));
        services.AddKnOwlControlPlaneDistributionStorageEntityFramework();
        services.AddKnOwlDocumentationStorageEntityFramework(
            options.ConfigureDocumentationStorage ?? options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Control Plane documentation"));
        services.AddKnOwlSecurityStorageEntityFramework(
            options.ConfigureSecurityStorage ?? options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Control Plane security"));

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

        app.UseAuthorization();

        var options = app.Services.GetRequiredService<KnOwlControlPlaneBootstrapOptions>();

        app.MapStaticAssets();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapButterMorphDesigner(options.ButterMorphPath);
        app.MapKnOwlControlPlaneApi(options.Authorization);
        app.MapKnOwlControlPlaneContractCatalogEndpoints();
        app.MapKnOwlArtifactDeliveryEndpoints();
        app.MapKnOwlDocumentationApi(options.Authorization);
        app.MapRazorPages().WithStaticAssets();

        return app;
    }

    private static void CopyList(IReadOnlyCollection<string> source, List<string> target)
    {
        target.Clear();
        target.AddRange(source);
    }

    private static Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> ThrowMissingStorageConfiguration(string storageName)
        => _ => throw new InvalidOperationException(
            $"KnOwl {storageName} storage requires an EF Core provider configuration. Set ConfigureStorage or ConfigureSecurityStorage in the bootstrap options.");
}

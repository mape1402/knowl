using ButterMorph.Web.Razor;
using KnOwl.ControlPlane.Api;
using KnOwl.ControlPlane.Application;
using KnOwl.ControlPlane.Application.Distribution;
using KnOwl.ControlPlane.Bootstrap.Configuration;
using KnOwl.ControlPlane.Bootstrap.Contracts;
using KnOwl.ControlPlane.Bootstrap.Distribution;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Storage.EntityFramework;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution;
using KnOwl.ControlPlane.WebUI;
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
        services.AddKnOwlControlPlaneWebUI();
        services.AddHealthChecks();
        services.AddKnOwlControlPlaneApplication();
        services.AddKnOwlControlPlaneDistributionApplication();

        var connectionString = KnOwlSqlConnectionStringFactory.Create(configuration);
        services.AddKnOwlControlPlaneStorageEntityFramework(
            connectionString,
            options.MigrationsAssembly ?? typeof(KnOwlDbContext).Assembly.GetName().Name!);
        services.AddKnOwlControlPlaneDistributionStorageEntityFramework();

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
        app.UseAuthorization();

        var options = app.Services.GetRequiredService<KnOwlControlPlaneBootstrapOptions>();

        app.MapStaticAssets();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapButterMorphDesigner(options.ButterMorphPath);
        app.MapKnOwlControlPlaneApi(options.ApiAuthorizationPolicy);
        app.MapKnOwlControlPlaneContractCatalogEndpoints();
        app.MapKnOwlArtifactDeliveryEndpoints();
        app.MapRazorPages().WithStaticAssets();

        return app;
    }
}

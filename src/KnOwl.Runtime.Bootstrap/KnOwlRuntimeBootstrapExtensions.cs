using KnOwl.Runtime.Application;
using KnOwl.Runtime.Bootstrap.Configuration;
using KnOwl.Runtime.Bootstrap.Grpc;
using KnOwl.Runtime.Bootstrap.Runtime;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Runtime.Storage.EntityFramework;
using KnOwl.Runtime.WebUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddKnOwlRuntimeWebUI();
        services.AddHealthChecks();
        services.AddGrpc();
        services.Configure<KnOwlRuntimeArtifactPullOptions>(
            configuration.GetSection("Runtime:ArtifactPull"));
        services.AddHostedService<KnOwlRuntimeArtifactPullWorker>();

        var connectionString = KnOwlRuntimeSqlConnectionStringFactory.Create(configuration);
        services.AddKnOwlRuntimeStorageEntityFramework(
            connectionString,
            options.MigrationsAssembly ?? typeof(KnOwlRuntimeDbContext).Assembly.GetName().Name!);
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

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        app.MapKnOwlRuntimeEndpoints();
        app.MapGrpcService<RuntimeContractsGrpcService>();
        app.MapGet("/runtime/status", () => Results.Ok(new { service = "KnOwl.Runtime", status = "ok" }));
        app.MapRazorPages();

        return app;
    }
}

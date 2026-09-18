using KnOwl.Runtime.Api;
using KnOwl.Runtime.Application;
using KnOwl.Runtime.Bootstrap.Grpc;
using KnOwl.Runtime.Bootstrap.Runtime;
using KnOwl.Runtime.Storage.EntityFramework;
using KnOwl.Runtime.WebUI;
using KnOwl.Security;
using KnOwl.Security.Storage.EntityFramework;
using Microsoft.AspNetCore.Authentication;
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
        services.AddGrpc();
        services.Configure<KnOwlRuntimeArtifactPullOptions>(
            configuration.GetSection("Runtime:ArtifactPull"));
        services.AddHostedService<KnOwlRuntimeArtifactPullWorker>();

        services.AddKnOwlRuntimeStorageEntityFramework(options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Runtime"));
        services.AddKnOwlSecurityStorageEntityFramework(
            options.ConfigureSecurityStorage ?? options.ConfigureStorage ?? ThrowMissingStorageConfiguration("Runtime security"));
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

        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        var options = app.Services.GetRequiredService<KnOwlRuntimeBootstrapOptions>();
        app.MapKnOwlRuntimeApi(options.Authorization);
        app.MapKnOwlRuntimeEndpoints();
        app.MapGrpcService<RuntimeContractsGrpcService>();
        app.MapGet("/runtime/status", () => Results.Ok(new { service = "KnOwl.Runtime", status = "ok" }));
        app.MapRazorPages();

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

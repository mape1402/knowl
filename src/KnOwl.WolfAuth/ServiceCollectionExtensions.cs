using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.WolfAuth;

/// <summary>
/// Registers KnOwl integration services that sit on top of host-configured WolfAuth authentication.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enables KnOwl's WolfAuth integration. The host must configure WolfAuth and ASP.NET Core authentication separately.
    /// </summary>
    public static IServiceCollection UseWolfAuth(
        this IServiceCollection services,
        Action<KnOwlWolfAuthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new KnOwlWolfAuthOptions();
        configure?.Invoke(options);

        services.AddOptions<KnOwlWolfAuthOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        RegisterEnabledIntegration(services, options.Enabled);

        return services;
    }

    /// <summary>
    /// Enables KnOwl's WolfAuth integration from configuration. The host must configure WolfAuth and ASP.NET Core authentication separately.
    /// </summary>
    public static IServiceCollection UseWolfAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KnOwlWolfAuthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new KnOwlWolfAuthOptions();
        configuration.Bind(options);
        configure?.Invoke(options);

        services.AddOptions<KnOwlWolfAuthOptions>();
        services.Configure<KnOwlWolfAuthOptions>(configuration);
        if (configure is not null)
        {
            services.Configure(configure);
        }

        RegisterEnabledIntegration(services, options.Enabled);

        return services;
    }

    private static void RegisterEnabledIntegration(IServiceCollection services, bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        services.TryAddSingleton<IKnOwlWolfAuthRegistration, KnOwlWolfAuthRegistration>();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}

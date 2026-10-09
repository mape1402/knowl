using Microsoft.AspNetCore.Authorization;
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

        services.AddOptions<KnOwlWolfAuthOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IKnOwlWolfAuthRegistration, KnOwlWolfAuthRegistration>();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}

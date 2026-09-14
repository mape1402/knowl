using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.AspNetCore;

/// <summary>
/// Provides service registration for KnOwl ASP.NET Core integration.
/// </summary>
public static class KnOwlAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds KnOwl ASP.NET Core options to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional middleware configuration.</param>
    /// <returns>The same service collection for fluent configuration.</returns>
    public static IServiceCollection AddKnOwlAspNetCore(
        this IServiceCollection services,
        Action<KnOwlAspNetCoreOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is null)
            services.AddOptions<KnOwlAspNetCoreOptions>();
        else
            services.Configure(configure);

        return services;
    }
}

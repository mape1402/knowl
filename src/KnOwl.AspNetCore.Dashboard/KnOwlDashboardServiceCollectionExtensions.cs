using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.DataProtection;

namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Provides dependency injection registration for the KnOwl dashboard.
/// </summary>
public static class KnOwlDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Adds the KnOwl dashboard services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional dashboard configuration.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddKnOwlDashboard(
        this IServiceCollection services,
        Action<KnOwlDashboardOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is null)
            services.AddOptions<KnOwlDashboardOptions>();
        else
            services.Configure(configure);

        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.TryAddScoped<IKnOwlDashboardUserAccessor, KnOwlDashboardUserAccessor>();
        return services;
    }
}

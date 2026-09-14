using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.Messaging;

/// <summary>
/// Provides service registration for transport-neutral KnOwl messaging support.
/// </summary>
public static class KnOwlMessagingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the transport-neutral messaging adapter services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional messaging configuration.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddKnOwlMessaging(
        this IServiceCollection services,
        Action<KnOwlMessagingOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is null)
            services.AddOptions<KnOwlMessagingOptions>();
        else
            services.Configure(configure);

        services.TryAddScoped<IInboxMessageService, DefaultInboxMessageService>();
        return services;
    }
}

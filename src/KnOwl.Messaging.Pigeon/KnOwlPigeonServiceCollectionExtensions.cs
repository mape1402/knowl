using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pigeon.Messaging.Consuming.Dispatching;
using Pigeon.Messaging.Producing;
using KnOwl.Messaging;
using KnOwl.Mule;

namespace KnOwl.Messaging.Pigeon;

/// <summary>
/// Provides Pigeon registration extensions for KnOwl.
/// </summary>
public static class KnOwlPigeonServiceCollectionExtensions
{
    /// <summary>
    /// Adds KnOwl to the Pigeon consume decision and execution pipelines.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional Pigeon adapter configuration.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddKnOwlPigeon(
        this IServiceCollection services,
        Action<KnOwlPigeonOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddKnOwlMessaging();
        services.AddKnOwlMule();

        if (configure is null)
            services.AddOptions<KnOwlPigeonOptions>();
        else
            services.Configure(configure);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IConsumeDecisionInterceptor, KnOwlPigeonDecisionInterceptor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IConsumeExecutionInterceptor, KnOwlPigeonExecutionInterceptor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPublishDecisionInterceptor, KnOwlPigeonOutboxInterceptor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxTransportPublisher, KnOwlPigeonOutboxPublisher>());
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.Mule;

/// <summary>
/// Provides Mule registration extensions for KnOwl.
/// </summary>
public static class KnOwlMuleServiceCollectionExtensions
{
    /// <summary>
    /// Adds the KnOwl Mule deferred scheduler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddKnOwlMule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IInboxMuleScheduler, InboxMuleScheduler>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxDeferredScheduler, KnOwlOutboxMuleScheduler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IKnOwlOperationDeferredScheduler, KnOwlOperationMuleScheduler>());
        return services;
    }
}

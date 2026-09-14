using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl;

/// <summary>
/// Provides dependency injection registration for the KnOwl core services.
/// </summary>
public static class KnOwlServiceCollectionExtensions
{
    /// <summary>
    /// Adds the KnOwl core services to the service collection.
    /// </summary>
    public static IServiceCollection AddKnOwl(
        this IServiceCollection services,
        Action<KnOwlOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
            services.Configure(configure);
        else
            services.AddOptions<KnOwlOptions>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IInboxContextAccessor, AsyncLocalInboxContextAccessor>();
        services.TryAddSingleton<IInboxPayloadFingerprinter, DefaultInboxPayloadFingerprinter>();
        services.TryAddSingleton<IInboxTransactionRunner, SuppressAmbientTransactionInboxRunner>();
        services.TryAddSingleton<IInboxPolicyResolver, DefaultInboxPolicyResolver>();
        services.TryAddSingleton<IKnOwlOperationRegistry, DefaultKnOwlOperationRegistry>();
        services.TryAddSingleton<IOutboxContextAccessor, AsyncLocalOutboxContextAccessor>();
        services.TryAddSingleton<IOutboxEnvelopeSerializer, JsonOutboxEnvelopeSerializer>();
        services.TryAddSingleton<IOutboxProfileRegistry, DefaultOutboxProfileRegistry>();
        services.TryAddSingleton<IKnOwlEventSink, InMemoryKnOwlEventSink>();
        services.TryAddSingleton<IKnOwlEventPublisher>(provider => provider.GetRequiredService<IKnOwlEventSink>());
        services.TryAddSingleton<IKnOwlEventSubscriber>(provider => provider.GetRequiredService<IKnOwlEventSink>());
        services.AddSingleton<IInboxPayloadHasher, JsonInboxPayloadHasher>();
        services.AddScoped<IInboxService, DefaultInbox>();
        services.AddScoped<IOutboxService, DefaultOutboxService>();
        services.AddScoped<IKnOwlOperationService, DefaultKnOwlOperationService>();

        return services;
    }
}

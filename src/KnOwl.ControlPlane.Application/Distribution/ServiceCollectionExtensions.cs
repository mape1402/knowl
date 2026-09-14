using KnOwl.ControlPlane.Application.Distribution.Artifacts;
using KnOwl.ControlPlane.Application.Distribution.ArtifactDelivery;
using KnOwl.ControlPlane.Application.Distribution.ReleaseBundles;
using KnOwl.ControlPlane.Application.Distribution.Security;
using KnOwl.Contracts.Security;
using KnOwl.ControlPlane.Distribution.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.ControlPlane.Application.Distribution;

/// <summary>
/// Registers KnOwl distribution interaction services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services used to create and manage KnOwl contract artifacts and releases.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneDistributionApplication(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddHttpClient("KnOwlRuntimeDistribution");
        services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
        services.TryAddSingleton<IConnectionSecretHasher, Pbkdf2ConnectionSecretHasher>();
        services.TryAddSingleton<IConnectionSecretGenerator, SecureConnectionSecretGenerator>();
        services.TryAddSingleton<ITokenHashService, Sha256TokenHashService>();
        services.TryAddSingleton<IConnectionScopeFormatter, DefaultConnectionScopeFormatter>();
        services.TryAddSingleton<ConnectionTokenCacheKeyBuilder>();
        services.TryAddSingleton<ConnectionCredentialPackageSerializer>();
        services.TryAddSingleton<IControlPlaneRuntimeNodeSecretProtector, DataProtectionControlPlaneRuntimeNodeSecretProtector>();
        services.AddScoped<IContractArtifactBuilder, ContractArtifactBuilder>();
        services.AddScoped<IContractReleaseInteractionService, ContractReleaseInteractionService>();
        services.AddScoped<IContractReleaseExecutionService, ContractReleaseExecutionService>();
        services.AddScoped<IControlPlaneConnectionTokenIssuer, ControlPlaneConnectionTokenIssuer>();
        services.AddScoped<IControlPlaneConnectionTokenValidator, ControlPlaneConnectionTokenValidator>();
        services.AddScoped<IArtifactDeliveryEndpointAuthenticator, ArtifactDeliveryEndpointAuthenticator>();
        services.AddScoped<IRuntimeAccessTokenProvider>(provider =>
            new RuntimeAccessTokenProvider(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlRuntimeDistribution"),
                provider.GetRequiredService<IControlPlaneRuntimeNodeSecretProtector>(),
                provider.GetRequiredService<IConnectionScopeFormatter>(),
                provider.GetRequiredService<ConnectionTokenCacheKeyBuilder>(),
                provider.GetRequiredService<IDistributedCache>()));
        services.AddScoped<IRuntimeNodeConnectionInteractionService>(provider =>
            new RuntimeNodeConnectionInteractionService(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlRuntimeDistribution"),
                provider.GetRequiredService<IRuntimeNodeRepository>(),
                provider.GetRequiredService<IConnectionSecretGenerator>(),
                provider.GetRequiredService<IConnectionSecretHasher>(),
                provider.GetRequiredService<IConnectionScopeFormatter>(),
                provider.GetRequiredService<ConnectionCredentialPackageSerializer>(),
                provider.GetRequiredService<IControlPlaneRuntimeNodeSecretProtector>(),
                provider.GetRequiredService<IRuntimeAccessTokenProvider>()));
        services.AddScoped<IArtifactDeliveryInteractionService>(provider =>
            new ArtifactDeliveryInteractionService(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlRuntimeDistribution"),
                provider.GetRequiredService<IContractReleaseTargetRepository>(),
                provider.GetRequiredService<IContractReleaseRepository>(),
                provider.GetRequiredService<IRuntimeAccessTokenProvider>()));
        return services;
    }
}

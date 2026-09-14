using KnOwl.Contracts.Security;
using KnOwl.Runtime.Application.ArtifactDelivery;
using KnOwl.Runtime.Application.Catalog;
using KnOwl.Runtime.Application.Deployments;
using KnOwl.Runtime.Application.Security;
using KnOwl.Runtime.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.Runtime.Application;

/// <summary>
/// Registers KnOwl runtime interaction services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds runtime deployment, catalog and distribution security services for KnOwl contracts.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeApplication(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddHttpClient("KnOwlControlPlaneDistribution");
        services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
        services.TryAddSingleton<IConnectionSecretHasher, Pbkdf2ConnectionSecretHasher>();
        services.TryAddSingleton<IConnectionSecretGenerator, SecureConnectionSecretGenerator>();
        services.TryAddSingleton<ITokenHashService, Sha256TokenHashService>();
        services.TryAddSingleton<IConnectionScopeFormatter, DefaultConnectionScopeFormatter>();
        services.TryAddSingleton<ConnectionTokenCacheKeyBuilder>();
        services.TryAddSingleton<ConnectionCredentialPackageSerializer>();
        services.TryAddSingleton<IRuntimeDesignNodeSecretProtector, DataProtectionRuntimeDesignNodeSecretProtector>();
        services.AddScoped<IRuntimeContractDeploymentService, RuntimeContractDeploymentService>();
        services.AddScoped<IRuntimeContractCatalogService, RuntimeContractCatalogService>();
        services.AddScoped<IRuntimeConnectionTokenIssuer, RuntimeConnectionTokenIssuer>();
        services.AddScoped<IRuntimeConnectionTokenValidator, RuntimeConnectionTokenValidator>();
        services.AddScoped<IRuntimeArtifactDeliveryEndpointAuthenticator, RuntimeArtifactDeliveryEndpointAuthenticator>();
        services.AddScoped<IControlPlaneArtifactPullOrchestrator, ControlPlaneArtifactPullOrchestrator>();
        services.AddScoped<IControlPlaneAccessTokenProvider>(provider =>
            new ControlPlaneAccessTokenProvider(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlControlPlaneDistribution"),
                provider.GetRequiredService<IRuntimeDesignNodeSecretProtector>(),
                provider.GetRequiredService<IConnectionScopeFormatter>(),
                provider.GetRequiredService<ConnectionTokenCacheKeyBuilder>(),
                provider.GetRequiredService<IDistributedCache>()));
        services.AddScoped<IRuntimeDesignNodeConnectionService>(provider =>
            new RuntimeDesignNodeConnectionService(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlControlPlaneDistribution"),
                provider.GetRequiredService<IRuntimeDesignNodeRepository>(),
                provider.GetRequiredService<IConnectionSecretGenerator>(),
                provider.GetRequiredService<IConnectionSecretHasher>(),
                provider.GetRequiredService<IConnectionScopeFormatter>(),
                provider.GetRequiredService<ConnectionCredentialPackageSerializer>(),
                provider.GetRequiredService<IRuntimeDesignNodeSecretProtector>(),
                provider.GetRequiredService<IControlPlaneAccessTokenProvider>()));
        services.AddScoped<IControlPlaneArtifactPullService>(provider =>
            new ControlPlaneArtifactPullService(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("KnOwlControlPlaneDistribution"),
                provider.GetRequiredService<IRuntimeDesignNodeRepository>(),
                provider.GetRequiredService<IControlPlaneAccessTokenProvider>(),
                provider.GetRequiredService<IRuntimeContractDeploymentService>()));
        return services;
    }
}

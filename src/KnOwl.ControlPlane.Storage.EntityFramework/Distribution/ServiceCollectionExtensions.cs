using KnOwl.ControlPlane.Distribution.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Distribution.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.Storage.EntityFramework.Distribution;

/// <summary>
/// Registers SQL Server storage for KnOwl distribution artifacts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server repositories used by KnOwl distribution features.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneDistributionStorageEntityFramework(this IServiceCollection services)
    {
        services.AddScoped<IContractArtifactRepository, ContractArtifactRepository>();
        services.AddScoped<IContractReleaseRepository, ContractReleaseRepository>();
        services.AddScoped<IRuntimeEnvironmentRepository, RuntimeEnvironmentRepository>();
        services.AddScoped<IRuntimeNodeRepository, RuntimeNodeRepository>();
        services.AddScoped<IContractReleaseTargetRepository, ContractReleaseTargetRepository>();
        return services;
    }
}

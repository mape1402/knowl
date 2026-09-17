using KnOwl.Runtime.Storage;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Runtime.Storage.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Runtime.Storage.EntityFramework;

/// <summary>
/// Registers EF Core storage for KnOwl runtime contract artifacts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the KnOwl Runtime EF Core context and repository implementations using the provided DbContext configuration.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeStorageEntityFramework(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<KnOwlRuntimeDbContext>(configureDbContext);
        return services.AddKnOwlRuntimeStorageEntityFramework();
    }

    /// <summary>
    /// Adds repositories used by KnOwl runtime storage.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeStorageEntityFramework(this IServiceCollection services)
    {
        services.AddScoped<IRuntimeContractArtifactRepository, RuntimeContractArtifactRepository>();
        services.AddScoped<IRuntimeDesignNodeRepository, RuntimeDesignNodeRepository>();
        return services;
    }
}

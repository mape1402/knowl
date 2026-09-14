using KnOwl.Runtime.Storage;
using KnOwl.Runtime.Storage.EntityFramework.Data;
using KnOwl.Runtime.Storage.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Runtime.Storage.EntityFramework;

/// <summary>
/// Registers SQL Server storage for KnOwl runtime contract artifacts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the KnOwl Runtime EF Core context and SQL Server repositories.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeStorageEntityFramework(
        this IServiceCollection services,
        string connectionString,
        string? migrationsAssembly = null)
    {
        services.AddDbContext<KnOwlRuntimeDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    sqlOptions.MigrationsAssembly(migrationsAssembly);
                }
            });
        });

        return services.AddKnOwlRuntimeStorageEntityFramework();
    }

    /// <summary>
    /// Adds SQL Server repositories used by KnOwl runtime storage.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeStorageEntityFramework(this IServiceCollection services)
    {
        services.AddScoped<IRuntimeContractArtifactRepository, RuntimeContractArtifactRepository>();
        services.AddScoped<IRuntimeDesignNodeRepository, RuntimeDesignNodeRepository>();
        return services;
    }
}

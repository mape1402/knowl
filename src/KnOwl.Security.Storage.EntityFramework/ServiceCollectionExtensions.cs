using KnOwl.Security.Storage;
using KnOwl.Security.Storage.EntityFramework.Data;
using KnOwl.Security.Storage.EntityFramework.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Security.Storage.EntityFramework;

/// <summary>
/// Registers EF Core storage for KnOwl security state.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server EF Core storage for KnOwl security state.
    /// </summary>
    public static IServiceCollection AddKnOwlSecurityStorageEntityFramework(
        this IServiceCollection services,
        string connectionString,
        string migrationsAssembly)
    {
        services.AddDbContext<KnOwlSecurityDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

        services.AddScoped<IKnOwlSecurityStore, KnOwlSecurityStore>();
        return services;
    }
}

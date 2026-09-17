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
    /// Adds EF Core storage for KnOwl security state using the provided DbContext configuration.
    /// </summary>
    public static IServiceCollection AddKnOwlSecurityStorageEntityFramework(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<KnOwlSecurityDbContext>(configureDbContext);
        services.AddScoped<IKnOwlSecurityStore, KnOwlSecurityStore>();
        return services;
    }
}

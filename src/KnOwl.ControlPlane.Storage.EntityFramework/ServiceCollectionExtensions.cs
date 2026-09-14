using KnOwl.ControlPlane.Storage.EntityFramework.Design.Data;
using KnOwl.ControlPlane.Design.Storage;
using KnOwl.ControlPlane.Storage.EntityFramework.Design.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.Storage.EntityFramework;

/// <summary>
/// Registers SQL Server storage dependencies for KnOwl async contract features.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the KnOwl DbContext and SQL Server repository implementations using the provided DbContext configuration.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneStorageEntityFramework(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<KnOwlDbContext>(configureDbContext);
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ICommandRepository, CommandRepository>();
        services.AddScoped<ISchemaTypeRepository, SchemaTypeRepository>();
        services.AddScoped<IContractFieldMetadataRepository, ContractFieldMetadataRepository>();
        return services;
    }

    /// <summary>
    /// Registers SQL Server storage using a connection string and explicit migrations assembly.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneStorageEntityFramework(
        this IServiceCollection services,
        string? connectionString,
        string migrationsAssembly)
    {
        return services.AddKnOwlControlPlaneStorageEntityFramework(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));
    }
}

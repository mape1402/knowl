using KnOwl.Documentation.Storage;
using KnOwl.Documentation.Storage.EntityFramework.Data;
using KnOwl.Documentation.Storage.EntityFramework.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Documentation.Storage.EntityFramework;

/// <summary>
/// Registers provider-agnostic EF Core documentation storage.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKnOwlDocumentationStorageEntityFramework(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddDbContext<KnOwlDocumentationDbContext>(configureDbContext);
        services.AddScoped<IDocumentationRepository, DocumentationRepository>();
        services.AddScoped<IDocumentationContentStore, DatabaseDocumentationContentStore>();
        return services;
    }
}

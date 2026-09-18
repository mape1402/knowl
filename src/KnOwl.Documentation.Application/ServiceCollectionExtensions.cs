using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Documentation.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKnOwlDocumentationApplication(this IServiceCollection services, Action<DocumentationOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddScoped<IDocumentationInteractionService, DocumentationInteractionService>();
        services.AddSingleton<IDocumentationPdfRenderer, SimpleDocumentationPdfRenderer>();
        return services;
    }
}

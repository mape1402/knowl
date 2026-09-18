using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Runtime.WebUI;

/// <summary>
/// Registers the KnOwl Runtime Web UI module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Runtime Web UI services.
    /// </summary>
    public static IServiceCollection AddKnOwlRuntimeWebUI(
        this IServiceCollection services,
        Action<KnOwlRuntimeThemeOptions>? configure = null)
    {
        services.AddOptions<KnOwlRuntimeThemeOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}

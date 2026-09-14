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
    public static IServiceCollection AddKnOwlRuntimeWebUI(this IServiceCollection services)
    {
        return services;
    }
}

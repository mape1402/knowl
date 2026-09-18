using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.Documentation.WebUI;

/// <summary>
/// Registers reusable KnOwl Documentation Razor UI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Documentation Web UI module.
    /// </summary>
    public static IServiceCollection AddKnOwlDocumentationWebUI(this IServiceCollection services)
        => services;
}

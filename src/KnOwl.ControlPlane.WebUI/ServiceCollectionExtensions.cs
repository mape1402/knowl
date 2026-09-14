using KnOwl.ControlPlane.WebUI.ButterMorph;
using KnOwl.ControlPlane.WebUI.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.WebUI;

/// <summary>
/// Registers KnOwl Control Plane Web UI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Control Plane Web UI module and its ButterMorph designer adapters.
    /// </summary>
    public static IServiceCollection AddKnOwlControlPlaneWebUI(this IServiceCollection services)
    {
        services.AddScoped<IMenuService, MenuService>();
        services.AddKnOwlButterMorphDesigner();
        return services;
    }
}

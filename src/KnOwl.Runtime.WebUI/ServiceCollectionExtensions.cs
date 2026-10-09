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
        Action<KnOwlRuntimeThemeOptions>? configure = null,
        Action<KnOwlRuntimeHomeOptions>? configureHome = null)
    {
        services.AddOptions<KnOwlRuntimeThemeOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        var homeOptions = new KnOwlRuntimeHomeOptions();
        configureHome?.Invoke(homeOptions);
        services.AddOptions<KnOwlRuntimeHomeOptions>()
            .Configure(options => CopyHomeOptions(homeOptions, options));

        return services;
    }

    private static void CopyHomeOptions(KnOwlRuntimeHomeOptions source, KnOwlRuntimeHomeOptions target)
    {
        target.Path = source.Path;
        target.RedirectRootToPath = source.RedirectRootToPath;
        target.Title = source.Title;
        target.Subtitle = source.Subtitle;
        target.ArtifactsActionText = source.ArtifactsActionText;
        target.ControlPlanesActionText = source.ControlPlanesActionText;
        target.LatestArtifactsTitle = source.LatestArtifactsTitle;
        target.LatestArtifactsSubtitle = source.LatestArtifactsSubtitle;
        target.ControlPlanesTitle = source.ControlPlanesTitle;
        target.ControlPlanesSubtitle = source.ControlPlanesSubtitle;
        target.EndpointsTitle = source.EndpointsTitle;
        target.EndpointsSubtitle = source.EndpointsSubtitle;

        target.EndpointRoutes.Clear();
        foreach (var route in source.EndpointRoutes)
        {
            target.EndpointRoutes.Add(route);
        }
    }
}

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
    public static IServiceCollection AddKnOwlControlPlaneWebUI(
        this IServiceCollection services,
        Action<KnOwlControlPlaneThemeOptions>? configure = null)
    {
        var themeOptions = new KnOwlControlPlaneThemeOptions();
        configure?.Invoke(themeOptions);

        services.AddOptions<KnOwlControlPlaneThemeOptions>()
            .Configure(options => CopyThemeOptions(themeOptions, options));

        services.AddScoped<IMenuService, MenuService>();
        services.AddKnOwlButterMorphDesigner(themeOptions);
        return services;
    }

    private static void CopyThemeOptions(KnOwlControlPlaneThemeOptions source, KnOwlControlPlaneThemeOptions target)
    {
        target.Title = source.Title;
        target.Mode = source.Mode;
        target.IconCssClass = source.IconCssClass;
        target.IconImageUrl = source.IconImageUrl;
        CopyPalette(source.Light, target.Light);
        CopyPalette(source.Dark, target.Dark);
    }

    private static void CopyPalette(KnOwlControlPlaneThemePaletteOptions source, KnOwlControlPlaneThemePaletteOptions target)
    {
        target.PrimaryColor = source.PrimaryColor;
        target.PrimaryHoverColor = source.PrimaryHoverColor;
        target.SidebarBackgroundColor = source.SidebarBackgroundColor;
        target.SidebarBrandBackgroundColor = source.SidebarBrandBackgroundColor;
        target.SidebarTextColor = source.SidebarTextColor;
        target.SidebarMutedTextColor = source.SidebarMutedTextColor;
        target.ContentBackgroundColor = source.ContentBackgroundColor;
        target.SurfaceColor = source.SurfaceColor;
        target.TextColor = source.TextColor;
        target.MutedTextColor = source.MutedTextColor;
        target.BorderColor = source.BorderColor;
        target.SubtleBackgroundColor = source.SubtleBackgroundColor;
        target.CodeBackgroundColor = source.CodeBackgroundColor;
        target.CodeTextColor = source.CodeTextColor;
        target.ShadowColor = source.ShadowColor;
    }
}

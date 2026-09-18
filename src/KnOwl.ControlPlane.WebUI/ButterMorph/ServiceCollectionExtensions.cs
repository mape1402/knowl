using ButterMorph.DependencyInjection;
using ButterMorph.Design;
using ButterMorph.Json.Schema;
using ButterMorph.SchemaDesign;
using ButterMorph.Web.Razor;
using KnOwl.ControlPlane.WebUI;
using Microsoft.Extensions.DependencyInjection;

namespace KnOwl.ControlPlane.WebUI.ButterMorph;

/// <summary>
/// Registers ButterMorph designer services and KnOwl host adapters.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ButterMorph designer dependencies used by KnOwl schema design pages.
    /// </summary>
    public static IServiceCollection AddKnOwlButterMorphDesigner(this IServiceCollection services, KnOwlControlPlaneThemeOptions themeOptions)
    {
        services.AddButterMorph();
        services.AddButterMorphJsonSchema();
        services.AddButterMorphSchemaDesign();
        services.AddButterMorphDesign();
        services.AddButterMorphRazorDesigner(options => ConfigureDesignerTheme(options.Theme, themeOptions));
        services.AddSingleton<KnOwlButterMorphDraftStore>();
        services.AddScoped<IButterMorphSchemaTypeDesignerHost, KnOwlSchemaTypeDesignerHost>();
        services.AddScoped<IButterMorphFieldMetadataDesignerHost, KnOwlFieldMetadataDesignerHost>();
        services.AddScoped<IButterMorphPayloadSchemaDesignerHost, KnOwlPayloadSchemaDesignerHost>();
        return services;
    }

    private static void ConfigureDesignerTheme(
        ButterMorphDesignerThemeOptions designerTheme,
        KnOwlControlPlaneThemeOptions knowlTheme)
    {
        designerTheme.DefaultMode = knowlTheme.Mode == KnOwlThemeMode.Dark
            ? ButterMorphDesignerThemeMode.Dark
            : ButterMorphDesignerThemeMode.Light;
        designerTheme.Mode = designerTheme.DefaultMode;

        CopyPalette(knowlTheme.Light, designerTheme.Light, isDark: false);
        CopyPalette(knowlTheme.Dark, designerTheme.Dark, isDark: true);
    }

    private static void CopyPalette(
        KnOwlControlPlaneThemePaletteOptions source,
        ButterMorphDesignerThemePaletteOptions target,
        bool isDark)
    {
        target.PrimaryColor = source.PrimaryColor;
        target.PrimaryHoverColor = source.PrimaryHoverColor;
        target.PrimaryDarkColor = isDark ? source.PrimaryHoverColor : source.PrimaryColor;
        target.BackgroundColor = source.ContentBackgroundColor;
        target.SurfaceColor = source.SurfaceColor;
        target.SurfaceSoftColor = source.SubtleBackgroundColor;
        target.TextColor = source.TextColor;
        target.MutedTextColor = source.MutedTextColor;
        target.BorderColor = source.BorderColor;
        target.StrongBorderColor = source.BorderColor;
        target.DangerColor = isDark ? "#f87171" : "#dc2626";
        target.SidebarBackgroundColor = source.SidebarBackgroundColor;
        target.SidebarBrandBackgroundColor = source.SidebarBrandBackgroundColor;
        target.SidebarBorderColor = source.BorderColor;
        target.SidebarTextColor = source.SidebarTextColor;
        target.SidebarMutedTextColor = source.SidebarMutedTextColor;
        target.SidebarActiveTextColor = source.PrimaryColor;
    }
}

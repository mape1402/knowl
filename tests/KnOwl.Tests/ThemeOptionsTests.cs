using ControlPlaneThemeMode = KnOwl.ControlPlane.WebUI.KnOwlThemeMode;
using RuntimeThemeMode = KnOwl.Runtime.WebUI.KnOwlThemeMode;

namespace KnOwl.Tests;

public sealed class ThemeOptionsTests
{
    [Fact]
    public void ControlPlaneThemeUsesDarkDefaultsWhenDarkModeIsConfigured()
    {
        var options = new KnOwl.ControlPlane.WebUI.KnOwlControlPlaneThemeOptions
        {
            Mode = ControlPlaneThemeMode.Dark
        };

        var css = options.ToCssVariables();

        Assert.Equal("dark", options.CssMode);
        Assert.Contains("--knowl-color-scheme: dark;", css);
        Assert.Contains("--knowl-content-bg: #0f172a;", css);
        Assert.Contains("--knowl-surface: #1e293b;", css);
        Assert.Contains("--knowl-text: #e5e7eb;", css);
        Assert.Contains("--bs-body-bg: #0f172a;", css);
    }

    [Fact]
    public void ControlPlaneThemeKeepsHostConfiguredColorsInDarkMode()
    {
        var options = new KnOwl.ControlPlane.WebUI.KnOwlControlPlaneThemeOptions
        {
            Mode = ControlPlaneThemeMode.Dark,
            PrimaryColor = "#2563eb",
            SurfaceColor = "#111827"
        };

        var css = options.ToCssVariables();

        Assert.Contains("--knowl-primary: #2563eb;", css);
        Assert.Contains("--knowl-surface: #111827;", css);
    }

    [Fact]
    public void ControlPlaneThemeLegacySettersApplyToBothPalettesAndNormalizeColors()
    {
        var options = new KnOwl.ControlPlane.WebUI.KnOwlControlPlaneThemeOptions
        {
            PrimaryColor = "#abc",
            PrimaryHoverColor = "#12345678",
            SidebarBackgroundColor = "#101010",
            SidebarBrandBackgroundColor = "#202020",
            SidebarTextColor = "#303030",
            SidebarMutedTextColor = "#404040",
            ContentBackgroundColor = "#505050",
            SurfaceColor = "#606060",
            TextColor = "#707070"
        };
        options.Light.MutedTextColor = "invalid";
        options.Light.BorderColor = "bad";
        options.Light.SubtleBackgroundColor = "";
        options.Light.CodeBackgroundColor = "#111111";
        options.Light.CodeTextColor = "#eeeeee";
        options.Light.ShadowColor = "  rgba(1, 2, 3, 0.4)  ";

        var css = options.ToCssVariables();
        var darkCss = options.ToCssThemeRules();

        Assert.Equal("#abc", options.Light.PrimaryColor);
        Assert.Equal("#abc", options.Dark.PrimaryColor);
        Assert.Equal("#12345678", options.Light.PrimaryHoverColor);
        Assert.Equal("#12345678", options.Dark.PrimaryHoverColor);
        Assert.Equal("#101010", options.Dark.SidebarBackgroundColor);
        Assert.Equal("#202020", options.Dark.SidebarBrandBackgroundColor);
        Assert.Equal("#303030", options.Dark.SidebarTextColor);
        Assert.Equal("#404040", options.Dark.SidebarMutedTextColor);
        Assert.Equal("#505050", options.Dark.ContentBackgroundColor);
        Assert.Equal("#606060", options.Dark.SurfaceColor);
        Assert.Equal("#707070", options.Dark.TextColor);
        Assert.Contains("--bs-primary-rgb: 170, 187, 204;", css);
        Assert.Contains("--bs-link-hover-color-rgb: 18, 52, 86;", css);
        Assert.Contains("--knowl-muted-text: #6b6e8c;", css);
        Assert.Contains("--knowl-shadow-color: rgba(1, 2, 3, 0.4);", css);
        Assert.Contains(":root[data-knowl-theme=\"light\"]", darkCss);
        Assert.Contains(":root[data-knowl-theme=\"dark\"]", darkCss);
    }

    [Fact]
    public void RuntimeThemeUsesDarkDefaultsWhenDarkModeIsConfigured()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            Mode = RuntimeThemeMode.Dark
        };

        var css = options.ToCssVariables();

        Assert.Equal("dark", options.CssMode);
        Assert.Contains("--knowl-color-scheme: dark;", css);
        Assert.Contains("--knowl-content-bg: #0f172a;", css);
        Assert.Contains("--knowl-surface: #1e293b;", css);
        Assert.Contains("--knowl-text: #e5e7eb;", css);
        Assert.Contains("--bs-body-color: #e5e7eb;", css);
    }

    [Fact]
    public void RuntimeThemeKeepsHostConfiguredColorsInDarkMode()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            Mode = RuntimeThemeMode.Dark,
            PrimaryColor = "#2563eb",
            ContentBackgroundColor = "#020617"
        };

        var css = options.ToCssVariables();

        Assert.Contains("--knowl-primary: #2563eb;", css);
        Assert.Contains("--knowl-content-bg: #020617;", css);
    }

    [Fact]
    public void RuntimeThemeUsesSidebarColorForBrandWhenBrandColorIsNotConfigured()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            Mode = RuntimeThemeMode.Light,
            SidebarBackgroundColor = "#0f2f5f"
        };

        var css = options.ToCssVariables();

        Assert.Contains("--knowl-sidebar-bg: #0f2f5f;", css);
        Assert.Contains("--knowl-sidebar-brand-bg: #0f2f5f;", css);
    }

    [Fact]
    public void RuntimeThemeKeepsConfiguredSidebarBrandColor()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            Mode = RuntimeThemeMode.Light,
            SidebarBackgroundColor = "#0f2f5f",
            SidebarBrandBackgroundColor = "#0b2347"
        };

        var css = options.ToCssVariables();

        Assert.Contains("--knowl-sidebar-bg: #0f2f5f;", css);
        Assert.Contains("--knowl-sidebar-brand-bg: #0b2347;", css);
    }

    [Fact]
    public void RuntimeThemeNormalizesShortAndAlphaHexColors()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            PrimaryColor = "#abc",
            PrimaryHoverColor = "#12345678"
        };

        var css = options.ToCssVariables();

        Assert.Contains("--bs-primary-rgb: 170, 187, 204;", css);
        Assert.Contains("--bs-link-hover-color-rgb: 18, 52, 86;", css);
    }

    [Fact]
    public void RuntimeThemeLegacyPropertiesMirrorBothPalettes()
    {
        var options = new KnOwl.Runtime.WebUI.KnOwlRuntimeThemeOptions
        {
            PrimaryColor = "#111111",
            PrimaryHoverColor = "#222222",
            SidebarBackgroundColor = "#333333",
            SidebarBrandBackgroundColor = "#444444",
            SidebarTextColor = "#555555",
            SidebarMutedTextColor = "#666666",
            ContentBackgroundColor = "#777777",
            SurfaceColor = "#888888",
            TextColor = "#999999"
        };

        Assert.Equal("#111111", options.PrimaryColor);
        Assert.Equal("#222222", options.PrimaryHoverColor);
        Assert.Equal("#333333", options.SidebarBackgroundColor);
        Assert.Equal("#444444", options.SidebarBrandBackgroundColor);
        Assert.Equal("#555555", options.SidebarTextColor);
        Assert.Equal("#666666", options.SidebarMutedTextColor);
        Assert.Equal("#777777", options.ContentBackgroundColor);
        Assert.Equal("#888888", options.SurfaceColor);
        Assert.Equal("#999999", options.TextColor);
        Assert.Equal("#555555", options.Dark.SidebarTextColor);
        Assert.Equal("#666666", options.Dark.SidebarMutedTextColor);
        Assert.Equal("#888888", options.Dark.SurfaceColor);
        Assert.Equal("#999999", options.Dark.TextColor);
    }

    [Fact]
    public void ControlPlaneThemeLegacyPropertiesExposeConfiguredValues()
    {
        var options = new KnOwl.ControlPlane.WebUI.KnOwlControlPlaneThemeOptions
        {
            PrimaryColor = "#111111",
            PrimaryHoverColor = "#222222",
            SidebarBackgroundColor = "#333333",
            SidebarBrandBackgroundColor = "#444444",
            SidebarTextColor = "#555555",
            SidebarMutedTextColor = "#666666",
            ContentBackgroundColor = "#777777",
            SurfaceColor = "#888888",
            TextColor = "#999999"
        };

        Assert.Equal("#111111", options.PrimaryColor);
        Assert.Equal("#222222", options.PrimaryHoverColor);
        Assert.Equal("#333333", options.SidebarBackgroundColor);
        Assert.Equal("#444444", options.SidebarBrandBackgroundColor);
        Assert.Equal("#555555", options.SidebarTextColor);
        Assert.Equal("#666666", options.SidebarMutedTextColor);
        Assert.Equal("#777777", options.ContentBackgroundColor);
        Assert.Equal("#888888", options.SurfaceColor);
        Assert.Equal("#999999", options.TextColor);
    }
}

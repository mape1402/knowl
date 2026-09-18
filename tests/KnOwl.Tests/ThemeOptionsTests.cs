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
}

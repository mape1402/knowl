using System.Text.RegularExpressions;

namespace KnOwl.ControlPlane.WebUI;

/// <summary>
/// Configures the visual branding used by the KnOwl Control Plane Web UI.
/// </summary>
public sealed partial class KnOwlControlPlaneThemeOptions
{
    /// <summary>
    /// Gets or sets the product title rendered in the sidebar brand and browser title.
    /// </summary>
    public string Title { get; set; } = "KnOwl";

    /// <summary>
    /// Gets or sets the Bootstrap Icons class used for the sidebar brand icon when <see cref="IconImageUrl"/> is not set.
    /// </summary>
    public string IconCssClass { get; set; } = "bi bi-hexagon-fill";

    /// <summary>
    /// Gets or sets an optional image URL used as the sidebar brand icon.
    /// </summary>
    public string? IconImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the primary accent color used by buttons, links, and active states.
    /// </summary>
    public string PrimaryColor { get; set; } = "#7c3aed";

    /// <summary>
    /// Gets or sets the primary accent hover color.
    /// </summary>
    public string PrimaryHoverColor { get; set; } = "#6d28d9";

    /// <summary>
    /// Gets or sets the sidebar background color.
    /// </summary>
    public string SidebarBackgroundColor { get; set; } = "#4c1d95";

    /// <summary>
    /// Gets or sets the sidebar brand background color.
    /// </summary>
    public string SidebarBrandBackgroundColor { get; set; } = "#3b0764";

    /// <summary>
    /// Gets or sets the sidebar text color.
    /// </summary>
    public string SidebarTextColor { get; set; } = "#ffffff";

    /// <summary>
    /// Gets or sets the sidebar muted text color.
    /// </summary>
    public string SidebarMutedTextColor { get; set; } = "#ddd6fe";

    /// <summary>
    /// Gets or sets the main content background color.
    /// </summary>
    public string ContentBackgroundColor { get; set; } = "#f8f7ff";

    /// <summary>
    /// Gets or sets the surface color used by top bars, cards, and forms.
    /// </summary>
    public string SurfaceColor { get; set; } = "#ffffff";

    /// <summary>
    /// Gets or sets the main text color.
    /// </summary>
    public string TextColor { get; set; } = "#1f1433";

    /// <summary>
    /// Gets a CSS variable declaration block for the configured theme.
    /// </summary>
    public string ToCssVariables()
        => string.Join(' ', new[]
        {
            CssVar("--knowl-primary", PrimaryColor),
            CssVar("--knowl-primary-hover", PrimaryHoverColor),
            CssVar("--knowl-sidebar-bg", SidebarBackgroundColor),
            CssVar("--knowl-sidebar-brand-bg", SidebarBrandBackgroundColor),
            CssVar("--knowl-sidebar-text", SidebarTextColor),
            CssVar("--knowl-sidebar-muted", SidebarMutedTextColor),
            CssVar("--knowl-content-bg", ContentBackgroundColor),
            CssVar("--knowl-surface", SurfaceColor),
            CssVar("--knowl-text", TextColor)
        });

    private static string CssVar(string name, string value)
        => $"{name}: {NormalizeColor(value)};";

    private static string NormalizeColor(string value)
        => CssColorRegex().IsMatch(value) ? value : "#7c3aed";

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex CssColorRegex();
}

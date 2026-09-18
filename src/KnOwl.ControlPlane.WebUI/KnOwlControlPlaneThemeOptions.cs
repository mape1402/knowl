using System.Text.RegularExpressions;

namespace KnOwl.ControlPlane.WebUI;

/// <summary>
/// Configures the visual branding used by the KnOwl Control Plane Web UI.
/// </summary>
public sealed partial class KnOwlControlPlaneThemeOptions
{
    private const string LightPrimaryColor = "#7c3aed";
    private const string LightPrimaryHoverColor = "#6d28d9";
    private const string LightSidebarBackgroundColor = "#4c1d95";
    private const string LightSidebarBrandBackgroundColor = "#3b0764";
    private const string LightSidebarTextColor = "#ffffff";
    private const string LightSidebarMutedTextColor = "#ddd6fe";
    private const string LightContentBackgroundColor = "#f8f7ff";
    private const string LightSurfaceColor = "#ffffff";
    private const string LightTextColor = "#1f1433";
    private const string LightMutedTextColor = "#6b6e8c";
    private const string LightBorderColor = "#e2e3ef";
    private const string LightSubtleBackgroundColor = "#f4f4fb";
    private const string LightCodeBackgroundColor = "#141428";
    private const string LightCodeTextColor = "#e9ebff";
    private const string LightShadowColor = "rgba(26, 26, 46, 0.12)";

    private const string DarkPrimaryColor = "#8b5cf6";
    private const string DarkPrimaryHoverColor = "#a78bfa";
    private const string DarkSidebarBackgroundColor = "#111827";
    private const string DarkSidebarBrandBackgroundColor = "#0b1120";
    private const string DarkSidebarTextColor = "#f8fafc";
    private const string DarkSidebarMutedTextColor = "#c4b5fd";
    private const string DarkContentBackgroundColor = "#0f172a";
    private const string DarkSurfaceColor = "#1e293b";
    private const string DarkTextColor = "#e5e7eb";
    private const string DarkMutedTextColor = "#94a3b8";
    private const string DarkBorderColor = "#334155";
    private const string DarkSubtleBackgroundColor = "#172033";
    private const string DarkCodeBackgroundColor = "#020617";
    private const string DarkCodeTextColor = "#dbeafe";
    private const string DarkShadowColor = "rgba(2, 6, 23, 0.48)";

    /// <summary>
    /// Gets or sets the product title rendered in the sidebar brand and browser title.
    /// </summary>
    public string Title { get; set; } = "KnOwl";

    /// <summary>
    /// Gets or sets the configured color mode.
    /// </summary>
    public KnOwlThemeMode Mode { get; set; } = KnOwlThemeMode.Light;

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
    public string PrimaryColor { get; set; } = LightPrimaryColor;

    /// <summary>
    /// Gets or sets the primary accent hover color.
    /// </summary>
    public string PrimaryHoverColor { get; set; } = LightPrimaryHoverColor;

    /// <summary>
    /// Gets or sets the sidebar background color.
    /// </summary>
    public string SidebarBackgroundColor { get; set; } = LightSidebarBackgroundColor;

    /// <summary>
    /// Gets or sets the sidebar brand background color.
    /// </summary>
    public string SidebarBrandBackgroundColor { get; set; } = LightSidebarBrandBackgroundColor;

    /// <summary>
    /// Gets or sets the sidebar text color.
    /// </summary>
    public string SidebarTextColor { get; set; } = LightSidebarTextColor;

    /// <summary>
    /// Gets or sets the sidebar muted text color.
    /// </summary>
    public string SidebarMutedTextColor { get; set; } = LightSidebarMutedTextColor;

    /// <summary>
    /// Gets or sets the main content background color.
    /// </summary>
    public string ContentBackgroundColor { get; set; } = LightContentBackgroundColor;

    /// <summary>
    /// Gets or sets the surface color used by top bars, cards, and forms.
    /// </summary>
    public string SurfaceColor { get; set; } = LightSurfaceColor;

    /// <summary>
    /// Gets or sets the main text color.
    /// </summary>
    public string TextColor { get; set; } = LightTextColor;

    /// <summary>
    /// Gets the CSS color mode value used by Bootstrap and browser form controls.
    /// </summary>
    public string CssMode => Mode == KnOwlThemeMode.Dark ? "dark" : "light";

    /// <summary>
    /// Gets a CSS variable declaration block for the configured theme.
    /// </summary>
    public string ToCssVariables()
    {
        var primaryColor = ResolveColor(PrimaryColor, LightPrimaryColor, DarkPrimaryColor);
        var primaryHoverColor = ResolveColor(PrimaryHoverColor, LightPrimaryHoverColor, DarkPrimaryHoverColor);
        var sidebarBackgroundColor = ResolveColor(SidebarBackgroundColor, LightSidebarBackgroundColor, DarkSidebarBackgroundColor);
        var sidebarBrandBackgroundColor = ResolveColor(SidebarBrandBackgroundColor, LightSidebarBrandBackgroundColor, DarkSidebarBrandBackgroundColor);
        var sidebarTextColor = ResolveColor(SidebarTextColor, LightSidebarTextColor, DarkSidebarTextColor);
        var sidebarMutedTextColor = ResolveColor(SidebarMutedTextColor, LightSidebarMutedTextColor, DarkSidebarMutedTextColor);
        var contentBackgroundColor = ResolveColor(ContentBackgroundColor, LightContentBackgroundColor, DarkContentBackgroundColor);
        var surfaceColor = ResolveColor(SurfaceColor, LightSurfaceColor, DarkSurfaceColor);
        var textColor = ResolveColor(TextColor, LightTextColor, DarkTextColor);
        var mutedTextColor = Mode == KnOwlThemeMode.Dark ? DarkMutedTextColor : LightMutedTextColor;
        var borderColor = Mode == KnOwlThemeMode.Dark ? DarkBorderColor : LightBorderColor;
        var subtleBackgroundColor = Mode == KnOwlThemeMode.Dark ? DarkSubtleBackgroundColor : LightSubtleBackgroundColor;
        var codeBackgroundColor = Mode == KnOwlThemeMode.Dark ? DarkCodeBackgroundColor : LightCodeBackgroundColor;
        var codeTextColor = Mode == KnOwlThemeMode.Dark ? DarkCodeTextColor : LightCodeTextColor;
        var shadowColor = Mode == KnOwlThemeMode.Dark ? DarkShadowColor : LightShadowColor;
        var primaryRgb = ToRgb(primaryColor);
        var primaryHoverRgb = ToRgb(primaryHoverColor);

        return string.Join(' ', new[]
        {
            CssVarRaw("--knowl-color-scheme", CssMode),
            CssVar("--knowl-primary", primaryColor),
            CssVar("--knowl-primary-hover", primaryHoverColor),
            CssVar("--knowl-sidebar-bg", sidebarBackgroundColor),
            CssVar("--knowl-sidebar-brand-bg", sidebarBrandBackgroundColor),
            CssVar("--knowl-sidebar-text", sidebarTextColor),
            CssVar("--knowl-sidebar-muted", sidebarMutedTextColor),
            CssVar("--knowl-content-bg", contentBackgroundColor),
            CssVar("--knowl-surface", surfaceColor),
            CssVar("--knowl-text", textColor),
            CssVar("--knowl-muted-text", mutedTextColor),
            CssVar("--knowl-border", borderColor),
            CssVar("--knowl-subtle-bg", subtleBackgroundColor),
            CssVar("--knowl-code-bg", codeBackgroundColor),
            CssVar("--knowl-code-text", codeTextColor),
            CssVarRaw("--knowl-shadow-color", shadowColor),
            CssVar("--bs-primary", primaryColor),
            CssVarRaw("--bs-primary-rgb", primaryRgb),
            CssVar("--bs-body-bg", contentBackgroundColor),
            CssVar("--bs-body-color", textColor),
            CssVar("--bs-border-color", borderColor),
            CssVar("--bs-link-color", primaryColor),
            CssVarRaw("--bs-link-color-rgb", primaryRgb),
            CssVar("--bs-link-hover-color", primaryHoverColor),
            CssVarRaw("--bs-link-hover-color-rgb", primaryHoverRgb),
            CssVarRaw("--bs-focus-ring-color", $"rgba({primaryRgb}, 0.25)")
        });
    }

    private static string CssVar(string name, string value)
        => $"{name}: {NormalizeColor(value)};";

    private static string CssVarRaw(string name, string value)
        => $"{name}: {value};";

    private string ResolveColor(string value, string lightDefault, string darkDefault)
    {
        var normalized = NormalizeColor(value, lightDefault);
        if (Mode == KnOwlThemeMode.Dark && string.Equals(normalized, lightDefault, StringComparison.OrdinalIgnoreCase))
        {
            return darkDefault;
        }

        return normalized;
    }

    private static string NormalizeColor(string value)
        => NormalizeColor(value, LightPrimaryColor);

    private static string NormalizeColor(string value, string fallback)
        => CssColorRegex().IsMatch(value) ? value : fallback;

    private static string ToRgb(string color)
    {
        var hex = color.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(character => $"{character}{character}"));
        }

        if (hex.Length == 8)
        {
            hex = hex[..6];
        }

        var red = Convert.ToInt32(hex[..2], 16);
        var green = Convert.ToInt32(hex.Substring(2, 2), 16);
        var blue = Convert.ToInt32(hex.Substring(4, 2), 16);
        return $"{red}, {green}, {blue}";
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex CssColorRegex();
}

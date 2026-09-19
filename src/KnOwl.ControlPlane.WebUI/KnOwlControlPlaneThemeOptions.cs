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
    /// Initializes a new instance of the <see cref="KnOwlControlPlaneThemeOptions"/> class.
    /// </summary>
    public KnOwlControlPlaneThemeOptions()
    {
        Light = new KnOwlControlPlaneThemePaletteOptions
        {
            PrimaryColor = LightPrimaryColor,
            PrimaryHoverColor = LightPrimaryHoverColor,
            SidebarBackgroundColor = LightSidebarBackgroundColor,
            SidebarBrandBackgroundColor = LightSidebarBrandBackgroundColor,
            SidebarTextColor = LightSidebarTextColor,
            SidebarMutedTextColor = LightSidebarMutedTextColor,
            ContentBackgroundColor = LightContentBackgroundColor,
            SurfaceColor = LightSurfaceColor,
            TextColor = LightTextColor,
            MutedTextColor = LightMutedTextColor,
            BorderColor = LightBorderColor,
            SubtleBackgroundColor = LightSubtleBackgroundColor,
            CodeBackgroundColor = LightCodeBackgroundColor,
            CodeTextColor = LightCodeTextColor,
            ShadowColor = LightShadowColor
        };
        Dark = new KnOwlControlPlaneThemePaletteOptions
        {
            PrimaryColor = DarkPrimaryColor,
            PrimaryHoverColor = DarkPrimaryHoverColor,
            SidebarBackgroundColor = DarkSidebarBackgroundColor,
            SidebarBrandBackgroundColor = DarkSidebarBrandBackgroundColor,
            SidebarTextColor = DarkSidebarTextColor,
            SidebarMutedTextColor = DarkSidebarMutedTextColor,
            ContentBackgroundColor = DarkContentBackgroundColor,
            SurfaceColor = DarkSurfaceColor,
            TextColor = DarkTextColor,
            MutedTextColor = DarkMutedTextColor,
            BorderColor = DarkBorderColor,
            SubtleBackgroundColor = DarkSubtleBackgroundColor,
            CodeBackgroundColor = DarkCodeBackgroundColor,
            CodeTextColor = DarkCodeTextColor,
            ShadowColor = DarkShadowColor
        };
    }

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
    /// Gets the light color palette configured by the host.
    /// </summary>
    public KnOwlControlPlaneThemePaletteOptions Light { get; }

    /// <summary>
    /// Gets the dark color palette configured by the host.
    /// </summary>
    public KnOwlControlPlaneThemePaletteOptions Dark { get; }

    /// <summary>
    /// Gets or sets the primary accent color used by buttons, links, and active states.
    /// </summary>
    public string PrimaryColor
    {
        get => Light.PrimaryColor;
        set
        {
            Light.PrimaryColor = value;
            Dark.PrimaryColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the primary accent hover color.
    /// </summary>
    public string PrimaryHoverColor
    {
        get => Light.PrimaryHoverColor;
        set
        {
            Light.PrimaryHoverColor = value;
            Dark.PrimaryHoverColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the sidebar background color.
    /// </summary>
    public string SidebarBackgroundColor
    {
        get => Light.SidebarBackgroundColor;
        set
        {
            Light.SidebarBackgroundColor = value;
            Dark.SidebarBackgroundColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the sidebar brand background color.
    /// </summary>
    public string SidebarBrandBackgroundColor
    {
        get => Light.SidebarBrandBackgroundColor;
        set
        {
            Light.SidebarBrandBackgroundColor = value;
            Dark.SidebarBrandBackgroundColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the sidebar text color.
    /// </summary>
    public string SidebarTextColor
    {
        get => Light.SidebarTextColor;
        set
        {
            Light.SidebarTextColor = value;
            Dark.SidebarTextColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the sidebar muted text color.
    /// </summary>
    public string SidebarMutedTextColor
    {
        get => Light.SidebarMutedTextColor;
        set
        {
            Light.SidebarMutedTextColor = value;
            Dark.SidebarMutedTextColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the main content background color.
    /// </summary>
    public string ContentBackgroundColor
    {
        get => Light.ContentBackgroundColor;
        set
        {
            Light.ContentBackgroundColor = value;
            Dark.ContentBackgroundColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the surface color used by top bars, cards, and forms.
    /// </summary>
    public string SurfaceColor
    {
        get => Light.SurfaceColor;
        set
        {
            Light.SurfaceColor = value;
            Dark.SurfaceColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the main text color.
    /// </summary>
    public string TextColor
    {
        get => Light.TextColor;
        set
        {
            Light.TextColor = value;
            Dark.TextColor = value;
        }
    }

    /// <summary>
    /// Gets the CSS color mode value used by Bootstrap and browser form controls.
    /// </summary>
    public string CssMode => Mode == KnOwlThemeMode.Dark ? "dark" : "light";

    /// <summary>
    /// Gets a CSS variable declaration block for the configured theme.
    /// </summary>
    public string ToCssVariables() => ToCssVariables(Mode);

    /// <summary>
    /// Gets the CSS rules that expose both configured color palettes.
    /// </summary>
    public string ToCssThemeRules()
    {
        return string.Join(Environment.NewLine, [
            $":root[data-knowl-theme=\"light\"] {{{ToCssVariables(KnOwlThemeMode.Light)}}}",
            $":root[data-knowl-theme=\"dark\"] {{{ToCssVariables(KnOwlThemeMode.Dark)}}}"
        ]);
    }

    private string ToCssVariables(KnOwlThemeMode mode)
    {
        var palette = mode == KnOwlThemeMode.Dark ? Dark : Light;
        var fallback = mode == KnOwlThemeMode.Dark ? CreateDefaultDarkPalette() : CreateDefaultLightPalette();
        var primaryColor = ResolveColor(palette.PrimaryColor, fallback.PrimaryColor);
        var primaryHoverColor = ResolveColor(palette.PrimaryHoverColor, fallback.PrimaryHoverColor);
        var sidebarBackgroundColor = ResolveColor(palette.SidebarBackgroundColor, fallback.SidebarBackgroundColor);
        var sidebarBrandBackgroundColor = ResolveColor(palette.SidebarBrandBackgroundColor, fallback.SidebarBrandBackgroundColor);
        var sidebarTextColor = ResolveColor(palette.SidebarTextColor, fallback.SidebarTextColor);
        var sidebarMutedTextColor = ResolveColor(palette.SidebarMutedTextColor, fallback.SidebarMutedTextColor);
        var contentBackgroundColor = ResolveColor(palette.ContentBackgroundColor, fallback.ContentBackgroundColor);
        var surfaceColor = ResolveColor(palette.SurfaceColor, fallback.SurfaceColor);
        var textColor = ResolveColor(palette.TextColor, fallback.TextColor);
        var mutedTextColor = ResolveColor(palette.MutedTextColor, fallback.MutedTextColor);
        var borderColor = ResolveColor(palette.BorderColor, fallback.BorderColor);
        var subtleBackgroundColor = ResolveColor(palette.SubtleBackgroundColor, fallback.SubtleBackgroundColor);
        var codeBackgroundColor = ResolveColor(palette.CodeBackgroundColor, fallback.CodeBackgroundColor);
        var codeTextColor = ResolveColor(palette.CodeTextColor, fallback.CodeTextColor);
        var shadowColor = ResolveRawColor(palette.ShadowColor, fallback.ShadowColor);
        var primaryRgb = ToRgb(primaryColor);
        var primaryHoverRgb = ToRgb(primaryHoverColor);

        return string.Join(' ', new[]
        {
            CssVarRaw("--knowl-color-scheme", mode == KnOwlThemeMode.Dark ? "dark" : "light"),
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
            CssVar("--knowl-surface-muted", subtleBackgroundColor),
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

    private static string ResolveColor(string value, string fallback)
        => NormalizeColor(value, fallback);

    private static string ResolveRawColor(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

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

    private static KnOwlControlPlaneThemePaletteOptions CreateDefaultLightPalette()
        => new()
        {
            PrimaryColor = LightPrimaryColor,
            PrimaryHoverColor = LightPrimaryHoverColor,
            SidebarBackgroundColor = LightSidebarBackgroundColor,
            SidebarBrandBackgroundColor = LightSidebarBrandBackgroundColor,
            SidebarTextColor = LightSidebarTextColor,
            SidebarMutedTextColor = LightSidebarMutedTextColor,
            ContentBackgroundColor = LightContentBackgroundColor,
            SurfaceColor = LightSurfaceColor,
            TextColor = LightTextColor,
            MutedTextColor = LightMutedTextColor,
            BorderColor = LightBorderColor,
            SubtleBackgroundColor = LightSubtleBackgroundColor,
            CodeBackgroundColor = LightCodeBackgroundColor,
            CodeTextColor = LightCodeTextColor,
            ShadowColor = LightShadowColor
        };

    private static KnOwlControlPlaneThemePaletteOptions CreateDefaultDarkPalette()
        => new()
        {
            PrimaryColor = DarkPrimaryColor,
            PrimaryHoverColor = DarkPrimaryHoverColor,
            SidebarBackgroundColor = DarkSidebarBackgroundColor,
            SidebarBrandBackgroundColor = DarkSidebarBrandBackgroundColor,
            SidebarTextColor = DarkSidebarTextColor,
            SidebarMutedTextColor = DarkSidebarMutedTextColor,
            ContentBackgroundColor = DarkContentBackgroundColor,
            SurfaceColor = DarkSurfaceColor,
            TextColor = DarkTextColor,
            MutedTextColor = DarkMutedTextColor,
            BorderColor = DarkBorderColor,
            SubtleBackgroundColor = DarkSubtleBackgroundColor,
            CodeBackgroundColor = DarkCodeBackgroundColor,
            CodeTextColor = DarkCodeTextColor,
            ShadowColor = DarkShadowColor
        };
}

/// <summary>
/// Configures one KnOwl Control Plane color palette.
/// </summary>
public sealed class KnOwlControlPlaneThemePaletteOptions
{
    /// <summary>Gets or sets the primary accent color.</summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary accent hover color.</summary>
    public string PrimaryHoverColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the sidebar background color.</summary>
    public string SidebarBackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the sidebar brand background color.</summary>
    public string SidebarBrandBackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the sidebar text color.</summary>
    public string SidebarTextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the sidebar muted text color.</summary>
    public string SidebarMutedTextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the main content background color.</summary>
    public string ContentBackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the surface color used by top bars, cards, and forms.</summary>
    public string SurfaceColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the main text color.</summary>
    public string TextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets muted text color.</summary>
    public string MutedTextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets border color.</summary>
    public string BorderColor { get; set; } = string.Empty;

    /// <summary>Gets or sets subtle background color.</summary>
    public string SubtleBackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets code background color.</summary>
    public string CodeBackgroundColor { get; set; } = string.Empty;

    /// <summary>Gets or sets code text color.</summary>
    public string CodeTextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets shadow color.</summary>
    public string ShadowColor { get; set; } = string.Empty;
}

namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Configures KnOwl dashboard authentication.
/// </summary>
public sealed class KnOwlDashboardAuthenticationOptions
{
    /// <summary>
    /// Gets or sets the dashboard authentication mode.
    /// </summary>
    public KnOwlDashboardAuthenticationMode Mode { get; set; } = KnOwlDashboardAuthenticationMode.RootUser;

    /// <summary>
    /// Gets or sets the built-in root user configuration.
    /// </summary>
    public KnOwlDashboardRootUser RootUser { get; set; } = new();

    /// <summary>
    /// Gets or sets the cookie name used by built-in dashboard authentication.
    /// </summary>
    public string CookieName { get; set; } = "KnOwl.Dashboard";
}

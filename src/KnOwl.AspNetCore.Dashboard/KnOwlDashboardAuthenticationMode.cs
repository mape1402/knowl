namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Defines how the KnOwl dashboard authenticates users.
/// </summary>
public enum KnOwlDashboardAuthenticationMode
{
    /// <summary>
    /// Uses the configured dashboard root user and the built-in login page.
    /// </summary>
    RootUser = 0,

    /// <summary>
    /// Uses the current ASP.NET Core authenticated principal.
    /// </summary>
    AspNetCoreAuthentication = 1,

    /// <summary>
    /// Uses a custom <see cref="IKnOwlDashboardAuthenticator"/> implementation.
    /// </summary>
    Custom = 2
}

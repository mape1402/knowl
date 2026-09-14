using Microsoft.AspNetCore.Http;

namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Provides request details to a custom dashboard authenticator.
/// </summary>
public sealed class KnOwlDashboardAuthRequest
{
    /// <summary>
    /// Gets or sets the current HTTP context.
    /// </summary>
    public HttpContext HttpContext { get; set; }

    /// <summary>
    /// Gets or sets the username supplied by a login attempt.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password supplied by a login attempt.
    /// </summary>
    public string Password { get; set; }
}

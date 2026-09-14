namespace KnOwl.AspNetCore.Dashboard;

/// <summary>
/// Configures the KnOwl dashboard.
/// </summary>
public sealed class KnOwlDashboardOptions
{
    /// <summary>
    /// Gets the authentication options.
    /// </summary>
    public KnOwlDashboardAuthenticationOptions Authentication { get; } = new();

    /// <summary>
    /// Gets or sets how many recent records and events are loaded by default.
    /// </summary>
    public int RecentLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether payload values should be hidden from the dashboard.
    /// </summary>
    public bool RedactPayloads { get; set; } = true;
}

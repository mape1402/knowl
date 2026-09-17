namespace KnOwl.Runtime.Bootstrap;

/// <summary>
/// Configures KnOwl Runtime host composition.
/// </summary>
public sealed class KnOwlRuntimeBootstrapOptions
{
    /// <summary>
    /// Gets or sets the EF Core migrations assembly used by the host.
    /// </summary>
    public string? MigrationsAssembly { get; set; }

    /// <summary>
    /// Gets or sets the authorization policy applied to the Runtime REST API. Leave empty to let the host map the API without a policy.
    /// </summary>
    public string? ApiAuthorizationPolicy { get; set; }
}

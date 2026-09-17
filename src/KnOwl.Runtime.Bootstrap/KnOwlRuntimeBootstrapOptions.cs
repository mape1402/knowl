using KnOwl.Security;
using KnOwl.Security.Authorization;

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
    public string? ApiAuthorizationPolicy
    {
        get => Authorization.FallbackPolicy;
        set => Authorization.FallbackPolicy = value;
    }

    /// <summary>
    /// Gets security options used by provider-agnostic KnOwl authorization.
    /// </summary>
    public KnOwlSecurityOptions Security { get; } = new();

    /// <summary>
    /// Gets authorization policy names applied to the Runtime REST API.
    /// </summary>
    public KnOwlApiAuthorizationOptions Authorization { get; } = new();
}

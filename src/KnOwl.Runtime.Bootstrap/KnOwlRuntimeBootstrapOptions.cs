using KnOwl.Runtime.WebUI;
using KnOwl.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;

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
    /// Gets or sets the EF Core provider configuration used by Runtime storage.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureStorage { get; set; }

    /// <summary>
    /// Gets visual theme and branding options used by the Runtime Web UI.
    /// </summary>
    public KnOwlRuntimeThemeOptions Theme { get; } = new();

    /// <summary>
    /// Gets or sets the authorization policy applied to the Runtime REST API. Leave empty to let the host map the API without a policy.
    /// </summary>
    public string? ApiAuthorizationPolicy
    {
        get => Authorization.FallbackPolicy;
        set => Authorization.FallbackPolicy = value;
    }

    /// <summary>
    /// Gets authorization policy names applied to the Runtime REST API.
    /// </summary>
    public KnOwlApiAuthorizationOptions Authorization { get; } = new();
}

using KnOwl.Security;
using KnOwl.Security.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KnOwl.ControlPlane.Bootstrap;

/// <summary>
/// Configures KnOwl Control Plane host composition.
/// </summary>
public sealed class KnOwlControlPlaneBootstrapOptions
{
    /// <summary>
    /// Gets or sets the EF Core migrations assembly used by the host.
    /// </summary>
    public string? MigrationsAssembly { get; set; }

    /// <summary>
    /// Gets or sets the EF Core provider configuration used by Control Plane storage.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureStorage { get; set; }

    /// <summary>
    /// Gets or sets the EF Core provider configuration used by security storage. When omitted, the bootstrap uses the Control Plane storage configuration.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureSecurityStorage { get; set; }

    /// <summary>
    /// Gets or sets the EF Core provider configuration used by documentation storage. When omitted, the bootstrap uses the Control Plane storage configuration.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDocumentationStorage { get; set; }

    /// <summary>
    /// Gets or sets the ButterMorph designer path.
    /// </summary>
    public string ButterMorphPath { get; set; } = "/buttermorph";

    /// <summary>
    /// Gets or sets the authorization policy applied to the Control Plane REST API. Leave empty to let the host map the API without a policy.
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
    /// Gets authorization policy names applied to the Control Plane REST API.
    /// </summary>
    public KnOwlApiAuthorizationOptions Authorization { get; } = new();
}

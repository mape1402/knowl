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
    /// Gets or sets the ButterMorph designer path.
    /// </summary>
    public string ButterMorphPath { get; set; } = "/buttermorph";
}

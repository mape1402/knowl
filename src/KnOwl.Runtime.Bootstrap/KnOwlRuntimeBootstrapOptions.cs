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
}

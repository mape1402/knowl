namespace KnOwl.Runtime.WebUI;

/// <summary>
/// Configures the default Runtime home page and optional host-owned replacement route.
/// </summary>
public sealed class KnOwlRuntimeHomeOptions
{
    /// <summary>
    /// Gets or sets the route used by sidebar and brand navigation for Home.
    /// Set this to a host-owned route to replace KnOwl's default home page.
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Gets or sets a value indicating whether KnOwl's default root page redirects to <see cref="Path" /> when it is not "/".
    /// </summary>
    public bool RedirectRootToPath { get; set; } = true;

    /// <summary>
    /// Gets or sets the default Runtime overview title.
    /// </summary>
    public string Title { get; set; } = "Runtime Overview";

    /// <summary>
    /// Gets or sets the default Runtime overview subtitle.
    /// </summary>
    public string Subtitle { get; set; } = "Operational view of deployed contract artifacts and Control Plane connections.";

    /// <summary>
    /// Gets or sets the label for the artifact navigation action.
    /// </summary>
    public string ArtifactsActionText { get; set; } = "View artifacts";

    /// <summary>
    /// Gets or sets the label for the Control Plane navigation action.
    /// </summary>
    public string ControlPlanesActionText { get; set; } = "Control planes";

    /// <summary>
    /// Gets or sets the latest artifacts panel title.
    /// </summary>
    public string LatestArtifactsTitle { get; set; } = "Latest Artifacts";

    /// <summary>
    /// Gets or sets the latest artifacts panel subtitle.
    /// </summary>
    public string LatestArtifactsSubtitle { get; set; } = "Most recent deployments available in this Runtime.";

    /// <summary>
    /// Gets or sets the Control Planes panel title.
    /// </summary>
    public string ControlPlanesTitle { get; set; } = "Control Planes";

    /// <summary>
    /// Gets or sets the Control Planes panel subtitle.
    /// </summary>
    public string ControlPlanesSubtitle { get; set; } = "Configured sources for incoming releases.";

    /// <summary>
    /// Gets or sets the Runtime endpoints panel title.
    /// </summary>
    public string EndpointsTitle { get; set; } = "Runtime Endpoints";

    /// <summary>
    /// Gets or sets the Runtime endpoints panel subtitle.
    /// </summary>
    public string EndpointsSubtitle { get; set; } = "Use these routes to consume deployed contracts.";

    /// <summary>
    /// Gets endpoint samples shown on the default Runtime home page.
    /// </summary>
    public IList<string> EndpointRoutes { get; } =
    [
        "GET /runtime/contracts/artifacts",
        "GET /runtime/contracts/events/{eventKey}/versions/{versionNumber}",
        "GET /runtime/contracts/commands/{commandKey}/versions/{versionNumber}"
    ];
}

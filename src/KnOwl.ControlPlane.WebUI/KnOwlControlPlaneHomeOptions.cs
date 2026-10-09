namespace KnOwl.ControlPlane.WebUI;

/// <summary>
/// Configures the default Control Plane home page and optional host-owned replacement route.
/// </summary>
public sealed class KnOwlControlPlaneHomeOptions
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
    /// Gets or sets the home page title. Empty uses the configured theme title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the home page subtitle.
    /// </summary>
    public string Subtitle { get; set; } =
        "Centralized hub for technical documentation, APIs, asynchronous messaging contracts, and NuGet feeds.";

    /// <summary>
    /// Gets or sets a value indicating whether the quick access section is rendered.
    /// </summary>
    public bool ShowQuickAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the platform overview section is rendered.
    /// </summary>
    public bool ShowOverview { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the getting started section is rendered.
    /// </summary>
    public bool ShowGettingStarted { get; set; } = true;

    /// <summary>
    /// Gets quick access cards shown on the default home page.
    /// </summary>
    public IList<KnOwlHomeCardOptions> QuickAccessCards { get; } =
    [
        new("API Documentation", "Explore REST and gRPC contracts for each service with examples and schemas.", "/apis", "journal-code", "var(--knowl-primary)"),
        new("Async Contracts", "Catalog of events, commands, and messages for asynchronous service integration.", "/contracts/events", "arrow-left-right", "var(--knowl-primary)"),
        new("NuGet Feeds", "Internal packages, versioning, and consumption guidance for your projects.", "/nuget/packages", "box-seam", "var(--knowl-primary-hover)"),
        new("Technical Guides", "Architecture guides, design patterns, coding standards, and runbooks.", "/documentation/spaces", "book", "#059669")
    ];

    /// <summary>
    /// Gets overview metric cards shown on the default home page.
    /// </summary>
    public IList<KnOwlHomeMetricOptions> OverviewCards { get; } =
    [
        new("Registered APIs"),
        new("Async Contracts"),
        new("NuGet Packages"),
        new("Published Guides")
    ];

    /// <summary>
    /// Gets getting started items shown on the default home page.
    /// </summary>
    public IList<string> GettingStartedItems { get; } =
    [
        "<strong>Browse the catalog</strong> - use the sidebar to explore available APIs and contracts.",
        "<strong>Review the schemas</strong> - each endpoint includes request/response samples and error codes.",
        "<strong>Integrate packages</strong> - configure the internal NuGet feed and add shared packages to your project.",
        "<strong>Contribute</strong> - propose documentation changes or register new contracts from your team."
    ];
}

/// <summary>
/// Configures one home quick access card.
/// </summary>
public sealed record KnOwlHomeCardOptions(
    string Title,
    string Description,
    string Url,
    string Icon,
    string AccentColor);

/// <summary>
/// Configures one home metric card.
/// </summary>
public sealed record KnOwlHomeMetricOptions(
    string Label,
    string Value = "-");

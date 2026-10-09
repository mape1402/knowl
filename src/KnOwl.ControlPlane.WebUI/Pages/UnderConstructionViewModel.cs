namespace KnOwl.ControlPlane.WebUI.Pages;

public sealed record UnderConstructionViewModel(
    string Title,
    string Subtitle,
    string Icon,
    IReadOnlyList<string> UpcomingFeatures,
    string PrimaryLinkText,
    string PrimaryLinkUrl)
{
    public string StatusLabel { get; init; } = "Under construction";
}

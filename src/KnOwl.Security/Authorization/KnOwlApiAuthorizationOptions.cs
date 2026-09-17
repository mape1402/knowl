namespace KnOwl.Security.Authorization;

/// <summary>
/// Configures policy names applied to KnOwl API endpoint groups.
/// </summary>
public sealed class KnOwlApiAuthorizationOptions
{
    public string? FallbackPolicy { get; set; }
    public string? PortalPolicy { get; set; }
    public string? SecurityManagePolicy { get; set; }
    public string? SchemaTypesReadPolicy { get; set; }
    public string? SchemaTypesWritePolicy { get; set; }
    public string? MetadataReadPolicy { get; set; }
    public string? MetadataWritePolicy { get; set; }
    public string? EventsReadPolicy { get; set; }
    public string? EventsWritePolicy { get; set; }
    public string? CommandsReadPolicy { get; set; }
    public string? CommandsWritePolicy { get; set; }
    public string? ArtifactsReadPolicy { get; set; }
    public string? ArtifactsBuildPolicy { get; set; }
    public string? ReleasesReadPolicy { get; set; }
    public string? ReleasesCreatePolicy { get; set; }
    public string? ReleasesExecutePolicy { get; set; }
    public string? RuntimeNodesReadPolicy { get; set; }
    public string? RuntimeNodesManagePolicy { get; set; }
    public string? RuntimeConnectionsManagePolicy { get; set; }
    public string? RuntimeArtifactsReadPolicy { get; set; }
    public string? RuntimeArtifactsApplyPolicy { get; set; }
}

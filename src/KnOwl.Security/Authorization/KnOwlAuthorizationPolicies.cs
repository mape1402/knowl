namespace KnOwl.Security.Authorization;

/// <summary>
/// Defines stable ASP.NET Core authorization policy names for KnOwl.
/// </summary>
public static class KnOwlAuthorizationPolicies
{
    public const string PortalAccess = "KnOwl.Portal.Access";
    public const string SecurityManage = "KnOwl.Security.Manage";
    public const string SchemaTypesRead = "KnOwl.SchemaTypes.Read";
    public const string SchemaTypesWrite = "KnOwl.SchemaTypes.Write";
    public const string MetadataRead = "KnOwl.Metadata.Read";
    public const string MetadataWrite = "KnOwl.Metadata.Write";
    public const string EventsRead = "KnOwl.Events.Read";
    public const string EventsWrite = "KnOwl.Events.Write";
    public const string CommandsRead = "KnOwl.Commands.Read";
    public const string CommandsWrite = "KnOwl.Commands.Write";
    public const string ArtifactsRead = "KnOwl.Artifacts.Read";
    public const string ArtifactsBuild = "KnOwl.Artifacts.Build";
    public const string ReleasesRead = "KnOwl.Releases.Read";
    public const string ReleasesCreate = "KnOwl.Releases.Create";
    public const string ReleasesExecute = "KnOwl.Releases.Execute";
    public const string RuntimeNodesRead = "KnOwl.RuntimeNodes.Read";
    public const string RuntimeNodesManage = "KnOwl.RuntimeNodes.Manage";
    public const string RuntimeConnectionsManage = "KnOwl.RuntimeConnections.Manage";
    public const string RuntimeArtifactsRead = "KnOwl.RuntimeArtifacts.Read";
    public const string RuntimeArtifactsApply = "KnOwl.RuntimeArtifacts.Apply";
    public const string DocumentationRead = "KnOwl.Documentation.Read";
    public const string DocumentationWrite = "KnOwl.Documentation.Write";
    public const string DocumentationPublish = "KnOwl.Documentation.Publish";
    public const string DocumentationDownload = "KnOwl.Documentation.Download";
}

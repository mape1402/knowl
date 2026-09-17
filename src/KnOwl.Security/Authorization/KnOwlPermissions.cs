namespace KnOwl.Security.Authorization;

/// <summary>
/// Defines stable KnOwl permission names used by host authorization policies.
/// </summary>
public static class KnOwlPermissions
{
    /// <summary>Allows access to the KnOwl portal surface.</summary>
    public const string PortalAccess = "knowl.portal.access";

    /// <summary>Allows management of KnOwl subjects, roles, permissions, and external group mappings.</summary>
    public const string SecurityManage = "knowl.security.manage";

    /// <summary>Allows reading schema type definitions.</summary>
    public const string SchemaTypesRead = "knowl.schema-types.read";

    /// <summary>Allows creating and updating schema type definitions.</summary>
    public const string SchemaTypesWrite = "knowl.schema-types.write";

    /// <summary>Allows reading metadata field definitions.</summary>
    public const string MetadataRead = "knowl.metadata.read";

    /// <summary>Allows creating and updating metadata field definitions.</summary>
    public const string MetadataWrite = "knowl.metadata.write";

    /// <summary>Allows reading event contracts.</summary>
    public const string EventsRead = "knowl.events.read";

    /// <summary>Allows creating and updating event contracts.</summary>
    public const string EventsWrite = "knowl.events.write";

    /// <summary>Allows reading command contracts.</summary>
    public const string CommandsRead = "knowl.commands.read";

    /// <summary>Allows creating and updating command contracts.</summary>
    public const string CommandsWrite = "knowl.commands.write";

    /// <summary>Allows reading generated artifacts.</summary>
    public const string ArtifactsRead = "knowl.artifacts.read";

    /// <summary>Allows building generated artifacts from contract versions.</summary>
    public const string ArtifactsBuild = "knowl.artifacts.build";

    /// <summary>Allows reading release bundles.</summary>
    public const string ReleasesRead = "knowl.releases.read";

    /// <summary>Allows creating release bundles.</summary>
    public const string ReleasesCreate = "knowl.releases.create";

    /// <summary>Allows executing release delivery.</summary>
    public const string ReleasesExecute = "knowl.releases.execute";

    /// <summary>Allows reading runtime node configuration.</summary>
    public const string RuntimeNodesRead = "knowl.runtime-nodes.read";

    /// <summary>Allows managing runtime node configuration.</summary>
    public const string RuntimeNodesManage = "knowl.runtime-nodes.manage";

    /// <summary>Allows managing runtime and control plane connection credentials.</summary>
    public const string RuntimeConnectionsManage = "knowl.runtime-connections.manage";

    /// <summary>Allows reading runtime artifact catalogs.</summary>
    public const string RuntimeArtifactsRead = "knowl.runtime-artifacts.read";

    /// <summary>Allows applying pending runtime artifacts.</summary>
    public const string RuntimeArtifactsApply = "knowl.runtime-artifacts.apply";
}

namespace KnOwl.Security.Authorization;

/// <summary>
/// Expands built-in KnOwl roles into permissions.
/// </summary>
public static class KnOwlRolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PermissionsByRole =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [KnOwlRoles.Reader] = PermissionSet(
                KnOwlPermissions.PortalAccess,
                KnOwlPermissions.SchemaTypesRead,
                KnOwlPermissions.MetadataRead,
                KnOwlPermissions.EventsRead,
                KnOwlPermissions.CommandsRead,
                KnOwlPermissions.ArtifactsRead,
                KnOwlPermissions.ReleasesRead,
                KnOwlPermissions.RuntimeNodesRead,
                KnOwlPermissions.RuntimeArtifactsRead),

            [KnOwlRoles.Designer] = PermissionSet(
                KnOwlPermissions.PortalAccess,
                KnOwlPermissions.SchemaTypesRead,
                KnOwlPermissions.SchemaTypesWrite,
                KnOwlPermissions.MetadataRead,
                KnOwlPermissions.MetadataWrite,
                KnOwlPermissions.EventsRead,
                KnOwlPermissions.EventsWrite,
                KnOwlPermissions.CommandsRead,
                KnOwlPermissions.CommandsWrite,
                KnOwlPermissions.ArtifactsRead),

            [KnOwlRoles.ReleaseManager] = PermissionSet(
                KnOwlPermissions.PortalAccess,
                KnOwlPermissions.ArtifactsRead,
                KnOwlPermissions.ArtifactsBuild,
                KnOwlPermissions.ReleasesRead,
                KnOwlPermissions.ReleasesCreate,
                KnOwlPermissions.ReleasesExecute,
                KnOwlPermissions.RuntimeNodesRead),

            [KnOwlRoles.RuntimeOperator] = PermissionSet(
                KnOwlPermissions.PortalAccess,
                KnOwlPermissions.RuntimeNodesRead,
                KnOwlPermissions.RuntimeNodesManage,
                KnOwlPermissions.RuntimeConnectionsManage,
                KnOwlPermissions.RuntimeArtifactsRead,
                KnOwlPermissions.RuntimeArtifactsApply),

            [KnOwlRoles.SecurityAdmin] = PermissionSet(
                KnOwlPermissions.PortalAccess,
                KnOwlPermissions.SecurityManage),

            [KnOwlRoles.Admin] = PermissionSet("*")
        };

    /// <summary>
    /// Gets permissions assigned to a role.
    /// </summary>
    public static IReadOnlySet<string> GetPermissions(string role)
        => PermissionsByRole.TryGetValue(role, out var permissions)
            ? permissions
            : PermissionSet();

    private static IReadOnlySet<string> PermissionSet(params string[] permissions)
        => new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
}

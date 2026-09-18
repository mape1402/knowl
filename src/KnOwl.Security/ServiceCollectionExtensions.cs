using KnOwl.Security.Authorization;
using KnOwl.Security.Storage;
using KnOwl.Security.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnOwl.Security;

/// <summary>
/// Registers provider-agnostic KnOwl security services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds KnOwl security services and standard authorization policies.
    /// </summary>
    public static IServiceCollection AddKnOwlSecurity(
        this IServiceCollection services,
        Action<KnOwlSecurityOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<IKnOwlSubjectResolver, ClaimsKnOwlSubjectResolver>();
        services.AddScoped<IKnOwlAuthorizationService, KnOwlAuthorizationService>();
        services.AddScoped<IAuthorizationHandler, KnOwlPermissionAuthorizationHandler>();
        services.TryAddSingleton<IKnOwlSecurityStore, InMemoryKnOwlSecurityStore>();
        services.AddAuthorizationBuilder().AddKnOwlPolicies();
        return services;
    }

    /// <summary>
    /// Adds KnOwl authorization policies to an authorization builder.
    /// </summary>
    public static AuthorizationBuilder AddKnOwlPolicies(this AuthorizationBuilder builder)
    {
        AddPolicy(builder, KnOwlAuthorizationPolicies.PortalAccess, KnOwlPermissions.PortalAccess);
        AddPolicy(builder, KnOwlAuthorizationPolicies.SecurityManage, KnOwlPermissions.SecurityManage);
        AddPolicy(builder, KnOwlAuthorizationPolicies.SchemaTypesRead, KnOwlPermissions.SchemaTypesRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.SchemaTypesWrite, KnOwlPermissions.SchemaTypesWrite);
        AddPolicy(builder, KnOwlAuthorizationPolicies.MetadataRead, KnOwlPermissions.MetadataRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.MetadataWrite, KnOwlPermissions.MetadataWrite);
        AddPolicy(builder, KnOwlAuthorizationPolicies.EventsRead, KnOwlPermissions.EventsRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.EventsWrite, KnOwlPermissions.EventsWrite);
        AddPolicy(builder, KnOwlAuthorizationPolicies.CommandsRead, KnOwlPermissions.CommandsRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.CommandsWrite, KnOwlPermissions.CommandsWrite);
        AddPolicy(builder, KnOwlAuthorizationPolicies.ArtifactsRead, KnOwlPermissions.ArtifactsRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.ArtifactsBuild, KnOwlPermissions.ArtifactsBuild);
        AddPolicy(builder, KnOwlAuthorizationPolicies.ReleasesRead, KnOwlPermissions.ReleasesRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.ReleasesCreate, KnOwlPermissions.ReleasesCreate);
        AddPolicy(builder, KnOwlAuthorizationPolicies.ReleasesExecute, KnOwlPermissions.ReleasesExecute);
        AddPolicy(builder, KnOwlAuthorizationPolicies.RuntimeNodesRead, KnOwlPermissions.RuntimeNodesRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.RuntimeNodesManage, KnOwlPermissions.RuntimeNodesManage);
        AddPolicy(builder, KnOwlAuthorizationPolicies.RuntimeConnectionsManage, KnOwlPermissions.RuntimeConnectionsManage);
        AddPolicy(builder, KnOwlAuthorizationPolicies.RuntimeArtifactsRead, KnOwlPermissions.RuntimeArtifactsRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.RuntimeArtifactsApply, KnOwlPermissions.RuntimeArtifactsApply);
        AddPolicy(builder, KnOwlAuthorizationPolicies.DocumentationRead, KnOwlPermissions.DocumentationRead);
        AddPolicy(builder, KnOwlAuthorizationPolicies.DocumentationWrite, KnOwlPermissions.DocumentationWrite);
        AddPolicy(builder, KnOwlAuthorizationPolicies.DocumentationPublish, KnOwlPermissions.DocumentationPublish);
        AddPolicy(builder, KnOwlAuthorizationPolicies.DocumentationDownload, KnOwlPermissions.DocumentationDownload);
        return builder;
    }

    private static void AddPolicy(AuthorizationBuilder builder, string policyName, string permission)
    {
        builder.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new KnOwlPermissionRequirement(permission));
        });
    }
}

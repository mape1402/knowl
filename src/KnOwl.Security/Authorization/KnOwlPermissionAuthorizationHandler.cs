using Microsoft.AspNetCore.Authorization;

namespace KnOwl.Security.Authorization;

/// <summary>
/// Evaluates KnOwl permission requirements for ASP.NET Core authorization.
/// </summary>
public sealed class KnOwlPermissionAuthorizationHandler(IKnOwlAuthorizationService authorizationService)
    : AuthorizationHandler<KnOwlPermissionRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        KnOwlPermissionRequirement requirement)
    {
        if (await authorizationService.HasPermission(context.User, requirement.Permission, requirement.Scope))
        {
            context.Succeed(requirement);
        }
    }
}

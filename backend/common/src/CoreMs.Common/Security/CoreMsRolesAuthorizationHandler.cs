using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace CoreMs.Common.Security;

/// <summary>
/// Central role hierarchy handler. Grants access to roles that should bypass specific role checks.
/// Currently: SUPER_ADMIN passes any role requirement.
/// Extend here for additional hierarchy logic (e.g., _ADMIN includes _MANAGER, etc.).
/// </summary>
public class CoreMsRolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(CoreMsRoles.SuperAdmin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

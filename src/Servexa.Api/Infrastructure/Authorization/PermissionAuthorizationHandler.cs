using Microsoft.AspNetCore.Authorization;
using Servexa.Application.Common.Interfaces;

namespace Servexa.Api.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(ITenantContext tenantContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!tenantContext.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        if (tenantContext.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

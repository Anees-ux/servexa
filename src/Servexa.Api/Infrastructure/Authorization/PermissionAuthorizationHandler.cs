using Microsoft.AspNetCore.Authorization;
using Servexa.Application.Common.Interfaces;

namespace Servexa.Api.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(ITenantContext tenantContext) : AuthorizationHandler<PermissionRequirement>
{
    private static readonly char[] Separators = ['|'];

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!tenantContext.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        var permissions = requirement.Permission.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (permissions.Any(tenantContext.HasPermission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

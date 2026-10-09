using Microsoft.EntityFrameworkCore;
using Servexa.Application.Common.Interfaces;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;
using Servexa.Infrastructure.Persistence;

namespace Servexa.Infrastructure.Identity;

public sealed class TenantResolutionService(ServexaDbContext dbContext) : ITenantResolutionService
{
    public async Task<TenantResolutionResult> ResolveTenantMembershipAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            return new TenantResolutionResult(false, tenantId, userId, null, null, [], [], "Tenant does not exist.");
        }

        if (tenant.Status != TenantStatus.Active)
        {
            return new TenantResolutionResult(false, tenantId, userId, null, null, [], [], $"Tenant lifecycle status '{tenant.Status}' does not permit operations.");
        }

        var tenantUser = await dbContext.TenantUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);

        if (tenantUser == null)
        {
            return new TenantResolutionResult(false, tenantId, userId, null, null, [], [], "User is not a member of the requested tenant.");
        }

        if (tenantUser.Status != TenantUserStatus.Active)
        {
            return new TenantResolutionResult(false, tenantId, userId, null, null, [], [], $"User account status '{tenantUser.Status}' is not active.");
        }

        var (roles, permissions) = await GetRolesAndPermissionsAsync(tenantId, userId, cancellationToken);

        return new TenantResolutionResult(
            IsSuccess: true,
            TenantId: tenant.Id,
            UserId: tenantUser.Id,
            Email: tenantUser.NormalizedEmail,
            DisplayName: tenantUser.DisplayName,
            Roles: roles,
            Permissions: permissions);
    }

    public async Task<TenantResolutionResult> ResolveExternalIdentityAsync(
        string externalIssuer,
        string externalSubject,
        Guid? requestedTenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TenantUsers
            .AsNoTracking()
            .Where(u => u.ExternalIssuer == externalIssuer && u.ExternalSubject == externalSubject);

        if (requestedTenantId.HasValue)
        {
            query = query.Where(u => u.TenantId == requestedTenantId.Value);
        }

        var tenantUser = await query.FirstOrDefaultAsync(cancellationToken);
        if (tenantUser == null)
        {
            return new TenantResolutionResult(false, Guid.Empty, Guid.Empty, null, null, [], [], "No tenant membership found for authenticated external subject.");
        }

        return await ResolveTenantMembershipAsync(tenantUser.TenantId, tenantUser.Id, cancellationToken);
    }

    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetRolesAndPermissionsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var activeAssignments = await dbContext.RoleAssignments
            .AsNoTracking()
            .Where(ra => ra.TenantId == tenantId
                         && ra.UserId == userId
                         && ra.EffectiveFromUtc <= utcNow
                         && (ra.EffectiveToUtc == null || ra.EffectiveToUtc > utcNow))
            .ToListAsync(cancellationToken);

        if (activeAssignments.Count == 0)
        {
            return ([], []);
        }

        var roleIds = activeAssignments.Select(ra => ra.RoleId).Distinct().ToList();

        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && roleIds.Contains(r.Id) && r.Status == RoleStatus.Active)
            .ToListAsync(cancellationToken);

        var activeRoleIds = roles.Select(r => r.Id).ToList();

        var permissionIds = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => activeRoleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await dbContext.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id) && p.Status == PermissionStatus.Active)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roles.Select(r => r.Name).ToList(), permissions);
    }
}

using Microsoft.EntityFrameworkCore;
using Servexa.Application.Common.Interfaces;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Platform.Enums;

namespace Servexa.Infrastructure.Persistence;

public sealed class DevDataSeeder(ServexaDbContext dbContext) : IDevDataSeeder
{
    public static readonly Guid DevTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DevBranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid DevAdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DevDispatcherUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        // 1. Ensure Tenant exists
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == DevTenantId, cancellationToken);
        if (tenant == null)
        {
            tenant = new Tenant("ACME", "Acme Industrial Operations Inc", "Acme Operations", "UTC", "USD", id: DevTenantId);
            await dbContext.Tenants.AddAsync(tenant, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Ensure Branch exists
        var branch = await dbContext.Branches.FirstOrDefaultAsync(b => b.TenantId == DevTenantId && b.Id == DevBranchId, cancellationToken);
        if (branch == null)
        {
            branch = new Branch(DevTenantId, "HQ", "Headquarters Branch", "UTC", id: DevBranchId);
            await dbContext.Branches.AddAsync(branch, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Ensure Permissions catalogue exists
        var canonicalCodes = new[]
        {
            Capabilities.CustomerView,
            Capabilities.CustomerCreate,
            Capabilities.CustomerUpdate,
            Capabilities.SiteView,
            Capabilities.SiteCreate,
            Capabilities.SiteUpdate,
            Capabilities.AssetView,
            Capabilities.AssetCreate,
            Capabilities.AssetUpdate,
            Capabilities.WorkOrderView,
            Capabilities.WorkOrderCreate,
            Capabilities.WorkOrderComplete,
            Capabilities.BookingView,
            Capabilities.BookingCreate,
            Capabilities.BookingAssign,
            Capabilities.BookingDispatch,
            Capabilities.TechnicianExecute,
            Capabilities.ResourceView,
            Capabilities.ResourceManage,
            Capabilities.InvoiceView,
            Capabilities.InvoicePost
        };

        var existingPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var code in canonicalCodes)
        {
            if (!existingPermissions.Any(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                var perm = new Permission(code, $"Grants authority for {code}");
                await dbContext.Permissions.AddAsync(perm, cancellationToken);
                existingPermissions.Add(perm);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // 4. Ensure Roles exist
        var adminRole = await dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.TenantId == DevTenantId && r.Name == "OperationsAdmin", cancellationToken);

        if (adminRole == null)
        {
            adminRole = new Role(DevTenantId, "OperationsAdmin", isSystem: true);
            await dbContext.Roles.AddAsync(adminRole, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var perm in existingPermissions)
        {
            adminRole.AddPermission(perm.Id);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Ensure TenantUsers exist
        var adminUser = await dbContext.TenantUsers.FirstOrDefaultAsync(u => u.TenantId == DevTenantId && u.Id == DevAdminUserId, cancellationToken);
        if (adminUser == null)
        {
            adminUser = new TenantUser(
                tenantId: DevTenantId,
                externalIssuer: "https://identity.servexa.local",
                externalSubject: "dev-admin-subject",
                displayName: "Acme Operations Admin",
                normalizedEmail: "OPERATIONS@SERVEXA.LOCAL",
                defaultBranchId: DevBranchId,
                status: TenantUserStatus.Active,
                id: DevAdminUserId);
            await dbContext.TenantUsers.AddAsync(adminUser, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 6. Ensure RoleAssignment exists for Admin
        var assignment = await dbContext.RoleAssignments.FirstOrDefaultAsync(
            ra => ra.TenantId == DevTenantId && ra.UserId == DevAdminUserId && ra.RoleId == adminRole.Id, cancellationToken);

        if (assignment == null)
        {
            assignment = new RoleAssignment(
                tenantId: DevTenantId,
                userId: DevAdminUserId,
                roleId: adminRole.Id,
                effectiveFromUtc: DateTime.UtcNow.AddYears(-1));
            await dbContext.RoleAssignments.AddAsync(assignment, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 7. Ensure Dev Resource exists for Admin
        var devResource = await dbContext.Resources.FirstOrDefaultAsync(
            r => r.TenantId == DevTenantId && r.UserId == DevAdminUserId, cancellationToken);
        if (devResource == null)
        {
            devResource = new Domain.Scheduling.Entities.Resource(
                tenantId: DevTenantId,
                resourceCode: "TECH-001",
                displayName: "Acme Lead Technician (Admin)",
                resourceType: Domain.Scheduling.Enums.ResourceType.Technician,
                userId: DevAdminUserId,
                homeBranchId: DevBranchId);
            await dbContext.Resources.AddAsync(devResource, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

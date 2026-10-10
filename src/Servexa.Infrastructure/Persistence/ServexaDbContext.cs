using Microsoft.EntityFrameworkCore;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core DbContext for Servexa persistence and Unit of Work.
/// </summary>
public class ServexaDbContext(DbContextOptions<ServexaDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Territory> Territories => Set<Territory>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<ScopeAssignment> ScopeAssignments => Set<ScopeAssignment>();
    public DbSet<PolicySetting> PolicySettings => Set<PolicySetting>();
    public DbSet<NumberSeries> NumberSeries => Set<NumberSeries>();
    public DbSet<FileObject> FileObjects => Set<FileObject>();

    // Customers Module
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteAccountRelationship> SiteAccountRelationships => Set<SiteAccountRelationship>();

    // Assets Module
    public DbSet<Domain.Assets.Entities.EquipmentModel> EquipmentModels => Set<Domain.Assets.Entities.EquipmentModel>();
    public DbSet<Domain.Assets.Entities.Asset> Assets => Set<Domain.Assets.Entities.Asset>();
    public DbSet<Domain.Assets.Entities.AssetLifecycleEvent> AssetLifecycleEvents => Set<Domain.Assets.Entities.AssetLifecycleEvent>();

    // Service Module
    public DbSet<Domain.Service.Entities.WorkOrder> WorkOrders => Set<Domain.Service.Entities.WorkOrder>();
    public DbSet<Domain.Service.Entities.WorkOrderAsset> WorkOrderAssets => Set<Domain.Service.Entities.WorkOrderAsset>();
    public DbSet<Domain.Service.Entities.WorkOrderStatusHistory> WorkOrderStatusHistories => Set<Domain.Service.Entities.WorkOrderStatusHistory>();

    // Scheduling Module
    public DbSet<Domain.Scheduling.Entities.Resource> Resources => Set<Domain.Scheduling.Entities.Resource>();
    public DbSet<Domain.Scheduling.Entities.ResourceScheduleGuard> ResourceScheduleGuards => Set<Domain.Scheduling.Entities.ResourceScheduleGuard>();
    public DbSet<Domain.Scheduling.Entities.ResourceCommitment> ResourceCommitments => Set<Domain.Scheduling.Entities.ResourceCommitment>();
    public DbSet<Domain.Scheduling.Entities.ResourceAssignment> ResourceAssignments => Set<Domain.Scheduling.Entities.ResourceAssignment>();
    public DbSet<Domain.Scheduling.Entities.Booking> Bookings => Set<Domain.Scheduling.Entities.Booking>();
    public DbSet<Domain.Scheduling.Entities.BookingScheduleRevision> BookingScheduleRevisions => Set<Domain.Scheduling.Entities.BookingScheduleRevision>();
    public DbSet<Domain.Scheduling.Entities.BookingStatusHistory> BookingStatusHistories => Set<Domain.Scheduling.Entities.BookingStatusHistory>();
    public DbSet<Domain.Scheduling.Entities.SchedulingConflictLog> SchedulingConflictLogs => Set<Domain.Scheduling.Entities.SchedulingConflictLog>();

    // Field Execution Module
    public DbSet<Domain.Field.Entities.ExecutionSession> ExecutionSessions => Set<Domain.Field.Entities.ExecutionSession>();
    public DbSet<Domain.Field.Entities.ExecutionInterval> ExecutionIntervals => Set<Domain.Field.Entities.ExecutionInterval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations defined in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServexaDbContext).Assembly);
    }
}

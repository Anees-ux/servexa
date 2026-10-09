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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations defined in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServexaDbContext).Assembly);
    }
}

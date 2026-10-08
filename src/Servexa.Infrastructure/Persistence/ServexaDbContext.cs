using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations defined in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServexaDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;

namespace Servexa.Infrastructure.Persistence;

/// <summary>
/// Primary EF Core DbContext for Servexa persistence and Unit of Work.
/// </summary>
public class ServexaDbContext(DbContextOptions<ServexaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations defined in the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServexaDbContext).Assembly);
    }
}

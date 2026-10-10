using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class ResourceScheduleGuardConfiguration : IEntityTypeConfiguration<ResourceScheduleGuard>
{
    public void Configure(EntityTypeBuilder<ResourceScheduleGuard> builder)
    {
        builder.ToTable("ResourceScheduleGuards", "scheduling");

        builder.HasKey(g => new { g.TenantId, g.ResourceId });

        builder.Property(g => g.TouchedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(g => g.RowVersion)
            .IsRowVersion();
    }
}

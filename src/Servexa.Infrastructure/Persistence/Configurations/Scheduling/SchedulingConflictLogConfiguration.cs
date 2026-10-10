using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class SchedulingConflictLogConfiguration : IEntityTypeConfiguration<SchedulingConflictLog>
{
    public void Configure(EntityTypeBuilder<SchedulingConflictLog> builder)
    {
        builder.ToTable("SchedulingConflictLogs", "scheduling");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AttemptedStartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(c => c.AttemptedEndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(c => c.ConflictType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Reason)
            .HasMaxLength(500);

        builder.Property(c => c.AttemptedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.ResourceId, c.AttemptedAtUtc });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class ResourceAssignmentConfiguration : IEntityTypeConfiguration<ResourceAssignment>
{
    public void Configure(EntityTypeBuilder<ResourceAssignment> builder)
    {
        builder.ToTable("ResourceAssignments", "scheduling");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasAlternateKey(a => new { a.TenantId, a.Id });

        builder.Property(a => a.AssignmentRole)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired();

        builder.Property(a => a.PlannedStartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(a => a.PlannedEndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(a => a.DispatchedAtUtc)
            .HasPrecision(3);

        builder.Property(a => a.CompletedAtUtc)
            .HasPrecision(3);

        builder.Property(a => a.SelectionRationale)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(a => a.ModifiedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.HasIndex(a => new { a.TenantId, a.BookingId });

        builder.HasIndex(a => new { a.TenantId, a.ResourceId, a.Status, a.PlannedStartUtc });
    }
}

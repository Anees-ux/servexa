using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", "scheduling");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        // Tenant-safe alternate key
        builder.HasAlternateKey(b => new { b.TenantId, b.Id });

        builder.Property(b => b.BookingNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(b => b.SiteTimeZoneId)
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValue("UTC");

        builder.Property(b => b.Status)
            .IsRequired();

        builder.Property(b => b.DispatchStatus)
            .IsRequired();

        builder.Property(b => b.PlannedStartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(b => b.PlannedEndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.SchedulingNotes)
            .HasMaxLength(1000);

        builder.Property(b => b.DispatchedAtUtc)
            .HasPrecision(3);

        builder.Property(b => b.StartedAtUtc)
            .HasPrecision(3);

        builder.Property(b => b.CompletedAtUtc)
            .HasPrecision(3);

        builder.Property(b => b.CreatedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(b => b.ModifiedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.HasIndex(b => new { b.TenantId, b.BookingNumber })
            .IsUnique();

        builder.HasIndex(b => new { b.TenantId, b.WorkOrderId });

        builder.HasIndex(b => new { b.TenantId, b.Status, b.PlannedStartUtc });

        builder.HasIndex(b => new { b.TenantId, b.SiteId, b.PlannedStartUtc });

        // Tenant-isolated composite foreign key to WorkOrder
        builder.HasOne<WorkOrder>()
            .WithMany()
            .HasForeignKey(b => new { b.TenantId, b.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-isolated composite foreign key to Site
        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(b => new { b.TenantId, b.SiteId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Child navigations
        builder.HasMany(b => b.Assignments)
            .WithOne()
            .HasForeignKey(a => new { a.TenantId, a.BookingId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Revisions)
            .WithOne()
            .HasForeignKey(r => new { r.TenantId, r.BookingId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.StatusHistory)
            .WithOne()
            .HasForeignKey(h => new { h.TenantId, h.BookingId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

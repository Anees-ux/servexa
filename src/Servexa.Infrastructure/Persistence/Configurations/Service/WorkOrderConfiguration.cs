using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Service;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders", "service");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        // Tenant-safe alternate key
        builder.HasAlternateKey(w => new { w.TenantId, w.Id });

        builder.Property(w => w.TenantId).IsRequired();
        builder.Property(w => w.WorkOrderNumber).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(w => w.ServiceRequestId);
        builder.Property(w => w.ServiceAccountId).IsRequired();
        builder.Property(w => w.BillToAccountId).IsRequired();
        builder.Property(w => w.PrimarySiteId).IsRequired();
        builder.Property(w => w.WorkTypeCode).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(w => w.Priority).HasColumnType("smallint").IsRequired();
        builder.Property(w => w.OperationalStatus).HasColumnType("smallint").IsRequired();
        builder.Property(w => w.Summary).HasMaxLength(300).IsRequired();
        builder.Property(w => w.Description);
        builder.Property(w => w.PauseReasonCode).HasMaxLength(50).IsUnicode(false);
        builder.Property(w => w.PauseNote);
        builder.Property(w => w.BillToSnapshotJson).IsRequired();
        builder.Property(w => w.OperationallyCompletedAtUtc).HasPrecision(3);

        builder.Property(w => w.CreatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(w => w.ModifiedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(w => w.Version).IsRowVersion().IsRequired();

        // Unique constraint on (TenantId, WorkOrderNumber)
        builder.HasIndex(w => new { w.TenantId, w.WorkOrderNumber })
            .IsUnique();

        // Tenant-safe FKs to Customers (Account & Site)
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(w => new { w.TenantId, w.ServiceAccountId })
            .HasPrincipalKey(a => new { a.TenantId, a.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(w => new { w.TenantId, w.BillToAccountId })
            .HasPrincipalKey(a => new { a.TenantId, a.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(w => new { w.TenantId, w.PrimarySiteId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Child relationships
        builder.HasMany(w => w.Assets)
            .WithOne()
            .HasForeignKey(wa => new { wa.TenantId, wa.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.StatusHistory)
            .WithOne()
            .HasForeignKey(h => new { h.TenantId, h.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes per approved physical model
        builder.HasIndex(w => new { w.TenantId, w.OperationalStatus, w.Priority, w.CreatedAtUtc });
        builder.HasIndex(w => new { w.TenantId, w.PrimarySiteId, w.OperationalStatus });
        builder.HasIndex(w => new { w.TenantId, w.ServiceAccountId, w.OperationalStatus });
    }
}

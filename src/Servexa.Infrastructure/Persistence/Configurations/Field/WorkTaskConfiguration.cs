using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Field;

public sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("WorkTasks", "field");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id });

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkOrderId).IsRequired();
        builder.Property(x => x.WorkOrderScopeItemId);
        builder.Property(x => x.BookingId);
        builder.Property(x => x.AssignmentId);
        builder.Property(x => x.AssetId);
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.TaskType).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.Gate).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasPrecision(3);
        builder.Property(x => x.CompletedByUserId);
        builder.Property(x => x.SkipReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(x => x.ModifiedAtUtc).HasPrecision(3).IsRequired();

        builder.Property(x => x.Version)
            .IsRowVersion();

        // Foreign key to WorkOrder
        builder.HasOne<WorkOrder>()
            .WithMany()
            .HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Cascade);

        // Performance & tenant query indexes
        builder.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.Sequence });
        builder.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssignmentId });
        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

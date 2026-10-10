using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Service;

public sealed class WorkOrderScopeItemConfiguration : IEntityTypeConfiguration<WorkOrderScopeItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderScopeItem> builder)
    {
        builder.ToTable("WorkOrderScopeItems", "service");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasAlternateKey(x => new { x.TenantId, x.Id });

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkOrderId).IsRequired();
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.ScopeType).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.IsRequiredForCompletion).IsRequired();
        builder.Property(x => x.AssetId);
        builder.Property(x => x.FulfilledAtUtc).HasPrecision(3);
        builder.Property(x => x.FulfilledByUserId);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(x => x.ModifiedAtUtc).HasPrecision(3).IsRequired();

        // Foreign key to WorkOrder
        builder.HasOne<WorkOrder>()
            .WithMany(w => w.ScopeItems)
            .HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
            .HasPrincipalKey(w => new { w.TenantId, w.Id })
            .OnDelete(DeleteBehavior.Cascade);

        // Performance and constraint indexes
        builder.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.Sequence });
        builder.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.AssetId });
    }
}

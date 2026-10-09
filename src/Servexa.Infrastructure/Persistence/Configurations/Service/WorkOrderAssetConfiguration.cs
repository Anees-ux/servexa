using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Assets.Entities;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Service;

public sealed class WorkOrderAssetConfiguration : IEntityTypeConfiguration<WorkOrderAsset>
{
    public void Configure(EntityTypeBuilder<WorkOrderAsset> builder)
    {
        builder.ToTable("WorkOrderAssets", "service");

        builder.HasKey(wa => wa.Id);
        builder.Property(wa => wa.Id).ValueGeneratedNever();

        builder.Property(wa => wa.TenantId).IsRequired();
        builder.Property(wa => wa.WorkOrderId).IsRequired();
        builder.Property(wa => wa.AssetId).IsRequired();
        builder.Property(wa => wa.Role).HasColumnType("smallint").IsRequired();
        builder.Property(wa => wa.SiteIdAtTime).IsRequired();
        builder.Property(wa => wa.Status).HasColumnType("smallint").IsRequired();

        // Unique constraint on (TenantId, WorkOrderId, AssetId)
        builder.HasIndex(wa => new { wa.TenantId, wa.WorkOrderId, wa.AssetId })
            .IsUnique();

        // Tenant-safe FK to Asset
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(wa => new { wa.TenantId, wa.AssetId })
            .HasPrincipalKey(a => new { a.TenantId, a.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

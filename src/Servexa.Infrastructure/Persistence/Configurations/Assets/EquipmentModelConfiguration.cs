using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Assets.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Assets;

public sealed class EquipmentModelConfiguration : IEntityTypeConfiguration<EquipmentModel>
{
    public void Configure(EntityTypeBuilder<EquipmentModel> builder)
    {
        builder.ToTable("EquipmentModels", "assets");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        // Tenant-safe alternate key
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.ManufacturerName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ModelCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CategoryCode).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(e => e.TrackingPolicy).HasColumnType("smallint").IsRequired();
        builder.Property(e => e.Status).HasColumnType("smallint").IsRequired();

        builder.Property(e => e.CreatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(e => e.ModifiedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(e => e.Version).IsRowVersion().IsRequired();

        // Unique constraint on (TenantId, ManufacturerName, ModelCode)
        builder.HasIndex(e => new { e.TenantId, e.ManufacturerName, e.ModelCode })
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.CategoryCode });
    }
}

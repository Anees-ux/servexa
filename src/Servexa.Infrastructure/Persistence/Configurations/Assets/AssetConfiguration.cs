using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Assets.Entities;
using Servexa.Domain.Customers.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", "assets");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        // Tenant-safe alternate key
        builder.HasAlternateKey(a => new { a.TenantId, a.Id });

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.AssetNumber).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(a => a.EquipmentModelId).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(100).IsUnicode(false);
        builder.Property(a => a.CurrentSiteId);
        builder.Property(a => a.CurrentOwnerAccountId);
        builder.Property(a => a.Status).HasColumnType("smallint").IsRequired();

        builder.Property(a => a.InstalledAtUtc).HasPrecision(3);
        builder.Property(a => a.DecommissionedAtUtc).HasPrecision(3);
        builder.Property(a => a.CreatedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(a => a.ModifiedAtUtc).HasPrecision(3).IsRequired();
        builder.Property(a => a.Version).IsRowVersion().IsRequired();

        // Unique constraint on (TenantId, AssetNumber)
        builder.HasIndex(a => new { a.TenantId, a.AssetNumber })
            .IsUnique();

        // Tenant-safe FK to EquipmentModel
        builder.HasOne<EquipmentModel>()
            .WithMany()
            .HasForeignKey(a => new { a.TenantId, a.EquipmentModelId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe FK to Site (optional)
        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(a => new { a.TenantId, a.CurrentSiteId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe FK to Account (optional)
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(a => new { a.TenantId, a.CurrentOwnerAccountId })
            .HasPrincipalKey(acc => new { acc.TenantId, acc.Id })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.LifecycleEvents)
            .WithOne()
            .HasForeignKey(e => new { e.TenantId, e.AssetId })
            .HasPrincipalKey(a => new { a.TenantId, a.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.EquipmentModelId });
        builder.HasIndex(a => new { a.TenantId, a.CurrentSiteId });
        builder.HasIndex(a => new { a.TenantId, a.CurrentOwnerAccountId });
        builder.HasIndex(a => new { a.TenantId, a.Status });
    }
}

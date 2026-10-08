using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class TerritoryConfiguration : IEntityTypeConfiguration<Territory>
{
    public void Configure(EntityTypeBuilder<Territory> builder)
    {
        builder.ToTable("Territories", "platform");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(t => new { t.TenantId, t.Id });

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.HasIndex(t => new { t.TenantId, t.Code })
            .IsUnique();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(t => t.ParentTerritoryId)
            .IsRequired(false);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(t => t.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(t => t.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe self-referential foreign key for hierarchical territory structures
        builder.HasOne<Territory>()
            .WithMany()
            .HasForeignKey(t => new { t.TenantId, t.ParentTerritoryId })
            .HasPrincipalKey(t => new { t.TenantId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

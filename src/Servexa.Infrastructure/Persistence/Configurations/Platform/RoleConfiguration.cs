using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "platform");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(r => new { r.TenantId, r.Id });

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(true); // nvarchar(100)

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique();

        builder.Property(r => r.IsSystem)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child permissions navigation mapping
        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

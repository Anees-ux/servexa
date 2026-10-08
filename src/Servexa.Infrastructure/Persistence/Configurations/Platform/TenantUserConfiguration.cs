using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers", "platform");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(u => new { u.TenantId, u.Id });

        builder.Property(u => u.ExternalIssuer)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(u => u.ExternalSubject)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        // Unique constraint per (TenantId, ExternalIssuer, ExternalSubject)
        builder.HasIndex(u => new { u.TenantId, u.ExternalIssuer, u.ExternalSubject })
            .IsUnique();

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(u => u.NormalizedEmail)
            .IsRequired(false)
            .HasMaxLength(256)
            .IsUnicode(false); // varchar(256)

        // Index on normalized email for lookup within tenant
        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail });

        builder.Property(u => u.DefaultBranchId)
            .IsRequired(false);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(u => u.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(u => u.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional tenant-safe foreign key to Branch using composite alternate key (TenantId, Id) on Branches
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(u => new { u.TenantId, u.DefaultBranchId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

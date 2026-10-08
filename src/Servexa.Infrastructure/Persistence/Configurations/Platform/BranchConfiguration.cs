using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "platform");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(b => new { b.TenantId, b.Id });

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.HasIndex(b => new { b.TenantId, b.Code })
            .IsUnique();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(b => b.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(b => b.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(b => b.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(b => b.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(b => b.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

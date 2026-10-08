using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class FileObjectConfiguration : IEntityTypeConfiguration<FileObject>
{
    public void Configure(EntityTypeBuilder<FileObject> builder)
    {
        builder.ToTable("FileObjects", "platform");

        builder.HasKey(fo => fo.Id);

        builder.Property(fo => fo.Id)
            .ValueGeneratedNever();

        builder.Property(fo => fo.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(fo => new { fo.TenantId, fo.Id });

        builder.Property(fo => fo.OwnerType)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(fo => fo.OwnerId)
            .IsRequired();

        builder.Property(fo => fo.OriginalName)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(true); // nvarchar(255)

        builder.Property(fo => fo.ContentType)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(fo => fo.SizeBytes)
            .IsRequired();

        builder.Property(fo => fo.ContentHash)
            .IsRequired(false)
            .HasMaxLength(32)
            .IsFixedLength(); // binary(32)

        builder.Property(fo => fo.StorageKey)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(false); // varchar(255)

        builder.Property(fo => fo.Classification)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.Property(fo => fo.Visibility)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(fo => fo.UploadStatus)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(fo => fo.ScanStatus)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(fo => fo.UploadedByUserId)
            .IsRequired(false);

        builder.Property(fo => fo.UploadedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(fo => fo.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(fo => fo.RetentionState)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(fo => fo.Version)
            .IsRowVersion();

        // Unique index per tenant and storage key
        builder.HasIndex(fo => new { fo.TenantId, fo.StorageKey })
            .IsUnique();

        // Index on owner context lookup
        builder.HasIndex(fo => new { fo.TenantId, fo.OwnerType, fo.OwnerId });

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(fo => fo.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional tenant-safe foreign key to TenantUser
        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(fo => new { fo.TenantId, fo.UploadedByUserId })
            .HasPrincipalKey(u => new { u.TenantId, u.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

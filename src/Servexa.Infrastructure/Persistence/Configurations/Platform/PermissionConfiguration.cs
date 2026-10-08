using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "platform");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(250)
            .IsUnicode(true); // nvarchar(250)

        builder.Property(p => p.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(p => p.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");
    }
}

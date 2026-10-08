using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "platform");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.TenantCode)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.HasIndex(t => t.TenantCode)
            .IsUnique();

        builder.Property(t => t.LegalName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(t => t.DisplayName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(t => t.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(t => t.DefaultTimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(t => t.DefaultCurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength()
            .IsUnicode(false); // char(3)

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(t => t.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(t => t.Version)
            .IsRowVersion();
    }
}

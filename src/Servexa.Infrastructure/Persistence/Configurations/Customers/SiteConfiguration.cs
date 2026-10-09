using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Customers;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites", "customers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(s => new { s.TenantId, s.Id });

        builder.Property(s => s.SiteNumber)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.HasIndex(s => new { s.TenantId, s.SiteNumber })
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(s => s.BranchId)
            .IsRequired();

        builder.Property(s => s.TerritoryId)
            .IsRequired(false);

        builder.Property(s => s.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(s => s.AddressLine1)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(s => s.AddressLine2)
            .IsRequired(false)
            .HasMaxLength(200)
            .IsUnicode(true); // nvarchar(200)

        builder.Property(s => s.City)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(true); // nvarchar(100)

        builder.Property(s => s.StateProvince)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(true); // nvarchar(100)

        builder.Property(s => s.PostalCode)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false); // varchar(20)

        builder.Property(s => s.CountryCode)
            .IsRequired()
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsFixedLength(); // char(2)

        builder.Property(s => s.Latitude)
            .IsRequired(false)
            .HasColumnType("decimal(9,6)");

        builder.Property(s => s.Longitude)
            .IsRequired(false)
            .HasColumnType("decimal(9,6)");

        builder.Ignore(s => s.LongLongitude);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(s => s.AccessNotes)
            .IsRequired(false)
            .HasMaxLength(1000)
            .IsUnicode(true); // nvarchar(1000)

        builder.Property(s => s.HazardNotes)
            .IsRequired(false)
            .HasMaxLength(1000)
            .IsUnicode(true); // nvarchar(1000)

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(s => s.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(s => s.Version)
            .IsRowVersion();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to Branch using composite alternate key (TenantId, Id) on Branches
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.BranchId })
            .HasPrincipalKey(b => new { b.TenantId, b.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Optional tenant-safe foreign key to Territory using composite alternate key (TenantId, Id) on Territories
        builder.HasOne<Territory>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.TerritoryId })
            .HasPrincipalKey(t => new { t.TenantId, t.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class NumberSeriesConfiguration : IEntityTypeConfiguration<NumberSeries>
{
    public void Configure(EntityTypeBuilder<NumberSeries> builder)
    {
        builder.ToTable("NumberSeries", "platform");

        builder.HasKey(ns => ns.Id);

        builder.Property(ns => ns.Id)
            .ValueGeneratedNever();

        builder.Property(ns => ns.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(ns => new { ns.TenantId, ns.Id });

        builder.Property(ns => ns.SeriesKey)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.Property(ns => ns.ScopeKey)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.Property(ns => ns.PrefixPattern)
            .IsRequired(false)
            .HasMaxLength(50)
            .IsUnicode(false); // varchar(50)

        builder.Property(ns => ns.NextValue)
            .IsRequired();

        builder.Property(ns => ns.Status)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(ns => ns.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ns => ns.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ns => ns.Version)
            .IsRowVersion();

        // Unique index per tenant, series key, and scope key
        builder.HasIndex(ns => new { ns.TenantId, ns.SeriesKey, ns.ScopeKey })
            .IsUnique();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(ns => ns.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

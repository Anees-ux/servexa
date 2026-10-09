using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Customers;

public class SiteAccountRelationshipConfiguration : IEntityTypeConfiguration<SiteAccountRelationship>
{
    public void Configure(EntityTypeBuilder<SiteAccountRelationship> builder)
    {
        builder.ToTable("SiteAccountRelationships", "customers");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(r => new { r.TenantId, r.Id });

        builder.Property(r => r.SiteId)
            .IsRequired();

        builder.Property(r => r.AccountId)
            .IsRequired();

        builder.Property(r => r.RelationType)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(r => r.IsDefault)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.EffectiveFromUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.EffectiveToUtc)
            .IsRequired(false)
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.HasIndex(r => new { r.TenantId, r.SiteId, r.AccountId, r.RelationType, r.EffectiveFromUtc });

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to Site using composite alternate key (TenantId, Id) on Sites
        builder.HasOne<Site>()
            .WithMany()
            .HasForeignKey(r => new { r.TenantId, r.SiteId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to Account using composite alternate key (TenantId, Id) on Accounts
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => new { r.TenantId, r.AccountId })
            .HasPrincipalKey(a => new { a.TenantId, a.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

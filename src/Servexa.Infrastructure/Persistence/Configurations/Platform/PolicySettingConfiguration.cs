using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class PolicySettingConfiguration : IEntityTypeConfiguration<PolicySetting>
{
    public void Configure(EntityTypeBuilder<PolicySetting> builder)
    {
        builder.ToTable("PolicySettings", "platform");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id)
            .ValueGeneratedNever();

        builder.Property(ps => ps.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(ps => new { ps.TenantId, ps.Id });

        builder.Property(ps => ps.PolicyKey)
            .IsRequired()
            .HasMaxLength(100)
            .IsUnicode(false); // varchar(100)

        builder.Property(ps => ps.ScopeType)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(ps => ps.ScopeId)
            .IsRequired(false);

        builder.Property(ps => ps.ValueJson)
            .IsRequired()
            .IsUnicode(true); // nvarchar(max)

        builder.Property(ps => ps.EffectiveFromUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ps => ps.EffectiveToUtc)
            .IsRequired(false)
            .HasColumnType("datetime2(3)");

        builder.Property(ps => ps.VersionNumber)
            .IsRequired();

        builder.Property(ps => ps.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ps => ps.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ps => ps.Version)
            .IsRowVersion();

        // Unique index per tenant, policy key, scope type, scope id, and effective instant
        builder.HasIndex(ps => new { ps.TenantId, ps.PolicyKey, ps.ScopeType, ps.ScopeId, ps.EffectiveFromUtc })
            .IsUnique();

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(ps => ps.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

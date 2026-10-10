using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources", "scheduling");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.HasAlternateKey(r => new { r.TenantId, r.Id });

        builder.Property(r => r.ResourceCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.ResourceType)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired();

        builder.Property(r => r.ExclusiveCapacity)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.ModifiedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.HasIndex(r => new { r.TenantId, r.ResourceCode })
            .IsUnique();

        builder.HasIndex(r => new { r.TenantId, r.Status, r.ResourceType });

        builder.HasIndex(r => new { r.TenantId, r.UserId });
    }
}

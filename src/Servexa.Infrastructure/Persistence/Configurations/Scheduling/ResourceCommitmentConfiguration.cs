using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class ResourceCommitmentConfiguration : IEntityTypeConfiguration<ResourceCommitment>
{
    public void Configure(EntityTypeBuilder<ResourceCommitment> builder)
    {
        builder.ToTable("ResourceCommitments", "scheduling");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.CommitmentKind)
            .IsRequired();

        builder.Property(c => c.StartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(c => c.EndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(c => c.ReleasedAtUtc)
            .HasPrecision(3);

        builder.Property(c => c.ReleaseReason)
            .HasMaxLength(500);

        builder.HasIndex(c => new { c.TenantId, c.ResourceId, c.Status, c.StartUtc, c.EndUtc });

        builder.HasIndex(c => new { c.TenantId, c.ResourceAssignmentId });
    }
}

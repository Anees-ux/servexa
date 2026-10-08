using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments", "platform");

        builder.HasKey(ra => ra.Id);

        builder.Property(ra => ra.Id)
            .ValueGeneratedNever();

        builder.Property(ra => ra.TenantId)
            .IsRequired();

        // Alternate key enabling tenant-safe composite foreign keys
        builder.HasAlternateKey(ra => new { ra.TenantId, ra.Id });

        builder.Property(ra => ra.UserId)
            .IsRequired();

        builder.Property(ra => ra.RoleId)
            .IsRequired();

        builder.Property(ra => ra.EffectiveFromUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ra => ra.EffectiveToUtc)
            .IsRequired(false)
            .HasColumnType("datetime2(3)");

        builder.Property(ra => ra.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ra => ra.ModifiedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        builder.Property(ra => ra.Version)
            .IsRowVersion();

        // Index on (TenantId, UserId, RoleId)
        builder.HasIndex(ra => new { ra.TenantId, ra.UserId, ra.RoleId });

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(ra => ra.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to TenantUser using composite alternate key
        builder.HasOne<TenantUser>()
            .WithMany()
            .HasForeignKey(ra => new { ra.TenantId, ra.UserId })
            .HasPrincipalKey(u => new { u.TenantId, u.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to Role using composite alternate key
        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ra => new { ra.TenantId, ra.RoleId })
            .HasPrincipalKey(r => new { r.TenantId, r.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Scopes child collection mapping
        builder.HasMany(ra => ra.Scopes)
            .WithOne()
            .HasForeignKey(sa => new { sa.TenantId, sa.RoleAssignmentId })
            .HasPrincipalKey(ra => new { ra.TenantId, ra.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Platform.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Platform;

public class ScopeAssignmentConfiguration : IEntityTypeConfiguration<ScopeAssignment>
{
    public void Configure(EntityTypeBuilder<ScopeAssignment> builder)
    {
        builder.ToTable("ScopeAssignments", "platform");

        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Id)
            .ValueGeneratedNever();

        builder.Property(sa => sa.TenantId)
            .IsRequired();

        builder.Property(sa => sa.RoleAssignmentId)
            .IsRequired();

        builder.Property(sa => sa.ScopeType)
            .IsRequired()
            .HasColumnType("smallint");

        builder.Property(sa => sa.ScopeId)
            .IsRequired(false);

        builder.Property(sa => sa.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2(3)");

        // Supporting index for active grant evaluation
        builder.HasIndex(sa => new { sa.TenantId, sa.RoleAssignmentId, sa.ScopeType, sa.ScopeId });

        // Tenant foreign key with Restrict delete behavior
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(sa => sa.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-safe foreign key to RoleAssignment using composite alternate key
        builder.HasOne<RoleAssignment>()
            .WithMany(ra => ra.Scopes)
            .HasForeignKey(sa => new { sa.TenantId, sa.RoleAssignmentId })
            .HasPrincipalKey(ra => new { ra.TenantId, ra.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

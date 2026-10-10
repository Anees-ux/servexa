using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Field.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Field;

public sealed class ExecutionSessionConfiguration : IEntityTypeConfiguration<ExecutionSession>
{
    public void Configure(EntityTypeBuilder<ExecutionSession> builder)
    {
        builder.ToTable("ExecutionSessions", "field");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.HasAlternateKey(s => new { s.TenantId, s.Id });

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.StartedAtUtc)
            .HasPrecision(3);

        builder.Property(s => s.CompletedAtUtc)
            .HasPrecision(3);

        builder.Property(s => s.WorkSummary)
            .HasMaxLength(2000);

        builder.Property(s => s.CreatedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(s => s.ModifiedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.HasIndex(s => new { s.TenantId, s.ResourceAssignmentId })
            .IsUnique();

        builder.HasIndex(s => new { s.TenantId, s.BookingId });

        builder.HasIndex(s => new { s.TenantId, s.UserId, s.Status });

        builder.HasMany(s => s.Intervals)
            .WithOne()
            .HasForeignKey(i => new { i.TenantId, i.ExecutionSessionId })
            .HasPrincipalKey(s => new { s.TenantId, s.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

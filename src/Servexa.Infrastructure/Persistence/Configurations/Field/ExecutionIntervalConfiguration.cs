using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Field.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Field;

public sealed class ExecutionIntervalConfiguration : IEntityTypeConfiguration<ExecutionInterval>
{
    public void Configure(EntityTypeBuilder<ExecutionInterval> builder)
    {
        builder.ToTable("ExecutionIntervals", "field");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.IntervalType)
            .IsRequired();

        builder.Property(i => i.StartedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(i => i.EndedAtUtc)
            .HasPrecision(3);

        builder.Property(i => i.Reason)
            .HasMaxLength(500);

        builder.HasIndex(i => new { i.TenantId, i.ExecutionSessionId, i.StartedAtUtc });
    }
}

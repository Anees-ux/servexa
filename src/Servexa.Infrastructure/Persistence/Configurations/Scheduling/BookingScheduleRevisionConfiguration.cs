using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class BookingScheduleRevisionConfiguration : IEntityTypeConfiguration<BookingScheduleRevision>
{
    public void Configure(EntityTypeBuilder<BookingScheduleRevision> builder)
    {
        builder.ToTable("BookingScheduleRevisions", "scheduling");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PreviousStartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.PreviousEndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.NewStartUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.NewEndUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(r => r.ReasonCode)
            .HasMaxLength(100);

        builder.Property(r => r.Reason)
            .HasMaxLength(500);

        builder.Property(r => r.Initiator)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.ChangedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.BookingId, r.RevisionNo })
            .IsUnique();
    }
}

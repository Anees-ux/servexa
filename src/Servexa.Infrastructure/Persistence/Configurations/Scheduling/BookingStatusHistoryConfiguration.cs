using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Infrastructure.Persistence.Configurations.Scheduling;

public sealed class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("BookingStatusHistories", "scheduling");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.FromStatus)
            .IsRequired();

        builder.Property(h => h.ToStatus)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasMaxLength(500);

        builder.Property(h => h.Trigger)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.ChangedAtUtc)
            .HasPrecision(3)
            .IsRequired();

        builder.HasIndex(h => new { h.TenantId, h.BookingId, h.ChangedAtUtc });
    }
}

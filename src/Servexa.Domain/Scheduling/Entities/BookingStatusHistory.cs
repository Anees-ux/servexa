using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Append-only audit ledger for Booking lifecycle status transitions (LDM §9.7).
/// </summary>
public sealed class BookingStatusHistory
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public BookingStatus FromStatus { get; private set; }
    public BookingStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public string Trigger { get; private set; } = "UserCommand";
    public Guid? ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private BookingStatusHistory() { }

    public BookingStatusHistory(
        Guid tenantId,
        Guid bookingId,
        BookingStatus fromStatus,
        BookingStatus toStatus,
        string? reason = null,
        string trigger = "UserCommand",
        Guid? changedByUserId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty.", nameof(bookingId));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        BookingId = bookingId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason?.Trim();
        Trigger = string.IsNullOrWhiteSpace(trigger) ? "UserCommand" : trigger.Trim();
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = DateTime.UtcNow;
    }
}

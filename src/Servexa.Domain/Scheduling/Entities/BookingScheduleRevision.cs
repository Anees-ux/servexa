namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Append-only revision ledger for Booking schedule changes (LDM §9.7, Physical Model §12.9).
/// Booking history survives reschedule.
/// </summary>
public sealed class BookingScheduleRevision
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public int RevisionNo { get; private set; }
    public DateTime PreviousStartUtc { get; private set; }
    public DateTime PreviousEndUtc { get; private set; }
    public DateTime NewStartUtc { get; private set; }
    public DateTime NewEndUtc { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Reason { get; private set; }
    public string Initiator { get; private set; } = "Dispatcher";
    public Guid? ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private BookingScheduleRevision() { }

    public BookingScheduleRevision(
        Guid tenantId,
        Guid bookingId,
        int revisionNo,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        DateTime newStartUtc,
        DateTime newEndUtc,
        string? reasonCode = null,
        string? reason = null,
        string initiator = "Dispatcher",
        Guid? changedByUserId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty.", nameof(bookingId));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        BookingId = bookingId;
        RevisionNo = revisionNo;
        PreviousStartUtc = previousStartUtc;
        PreviousEndUtc = previousEndUtc;
        NewStartUtc = newStartUtc;
        NewEndUtc = newEndUtc;
        ReasonCode = reasonCode?.Trim();
        Reason = reason?.Trim();
        Initiator = string.IsNullOrWhiteSpace(initiator) ? "Dispatcher" : initiator.Trim();
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = DateTime.UtcNow;
    }
}

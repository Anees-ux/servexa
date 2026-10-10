using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Customer visit envelope aggregate root for a Work Order (LDM §9.6, Physical Model §12.5).
/// Distinct lifecycle from Work Order; supports multi-visit workflows.
/// </summary>
public sealed class Booking
{
    private readonly List<ResourceAssignment> _assignments = [];
    private readonly List<BookingScheduleRevision> _revisions = [];
    private readonly List<BookingStatusHistory> _statusHistory = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string BookingNumber { get; private set; } = string.Empty;
    public Guid WorkOrderId { get; private set; }
    public Guid SiteId { get; private set; }
    public DateTime PlannedStartUtc { get; private set; }
    public DateTime PlannedEndUtc { get; private set; }
    public string SiteTimeZoneId { get; private set; } = "UTC";
    public BookingStatus Status { get; private set; } = BookingStatus.Scheduled;
    public BookingDispatchStatus DispatchStatus { get; private set; } = BookingDispatchStatus.Unassigned;
    public int Sequence { get; private set; } = 1;
    public string? CancellationReason { get; private set; }
    public string? SchedulingNotes { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ResourceAssignment> Assignments => _assignments.AsReadOnly();
    public IReadOnlyCollection<BookingScheduleRevision> Revisions => _revisions.AsReadOnly();
    public IReadOnlyCollection<BookingStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Booking() { }

    public Booking(
        Guid tenantId,
        string bookingNumber,
        Guid workOrderId,
        Guid siteId,
        DateTime plannedStartUtc,
        DateTime plannedEndUtc,
        string siteTimeZoneId = "UTC",
        BookingStatus status = BookingStatus.Scheduled,
        int sequence = 1,
        string? schedulingNotes = null,
        Guid? createdByUserId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (workOrderId == Guid.Empty) throw new ArgumentException("WorkOrderId cannot be empty.", nameof(workOrderId));
        if (siteId == Guid.Empty) throw new ArgumentException("SiteId cannot be empty.", nameof(siteId));
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingNumber);
        if (plannedEndUtc <= plannedStartUtc) throw new ArgumentException("PlannedEndUtc must be strictly greater than PlannedStartUtc.", nameof(plannedEndUtc));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        BookingNumber = bookingNumber.Trim().ToUpperInvariant();
        WorkOrderId = workOrderId;
        SiteId = siteId;
        PlannedStartUtc = plannedStartUtc;
        PlannedEndUtc = plannedEndUtc;
        SiteTimeZoneId = string.IsNullOrWhiteSpace(siteTimeZoneId) ? "UTC" : siteTimeZoneId.Trim();
        Status = status;
        DispatchStatus = BookingDispatchStatus.Unassigned;
        Sequence = sequence > 0 ? sequence : 1;
        if (schedulingNotes != null && schedulingNotes.Trim().Length > 1000)
        {
            throw new ArgumentException("Scheduling notes cannot exceed 1000 characters.", nameof(schedulingNotes));
        }
        SchedulingNotes = schedulingNotes?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new BookingStatusHistory(
            tenantId: TenantId,
            bookingId: Id,
            fromStatus: status,
            toStatus: status,
            reason: "Initial booking schedule created",
            trigger: "BookingCreation",
            changedByUserId: createdByUserId));
    }

    public void AddAssignment(ResourceAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        if (assignment.BookingId != Id) throw new InvalidOperationException("Assignment BookingId does not match this Booking.");
        if (assignment.TenantId != TenantId) throw new InvalidOperationException("Cross-tenant assignment is prohibited.");

        _assignments.Add(assignment);
        if (DispatchStatus == BookingDispatchStatus.Unassigned)
        {
            DispatchStatus = BookingDispatchStatus.Assigned;
        }
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDispatchStatus(BookingDispatchStatus newDispatchStatus)
    {
        DispatchStatus = newDispatchStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Reschedule(
        DateTime newStartUtc,
        DateTime newEndUtc,
        string reason,
        string initiator = "Dispatcher",
        Guid? changedByUserId = null)
    {
        if (Status == BookingStatus.Completed || Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot reschedule a booking in status '{Status}'.");
        }
        if (newEndUtc <= newStartUtc)
        {
            throw new ArgumentException("New end time must be greater than new start time.", nameof(newEndUtc));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Trim().Length > 500)
        {
            throw new ArgumentException("Reschedule reason cannot exceed 500 characters.", nameof(reason));
        }

        var prevStart = PlannedStartUtc;
        var prevEnd = PlannedEndUtc;

        PlannedStartUtc = newStartUtc;
        PlannedEndUtc = newEndUtc;
        ModifiedAtUtc = DateTime.UtcNow;

        var revisionNo = _revisions.Count + 1;
        _revisions.Add(new BookingScheduleRevision(
            tenantId: TenantId,
            bookingId: Id,
            revisionNo: revisionNo,
            previousStartUtc: prevStart,
            previousEndUtc: prevEnd,
            newStartUtc: newStartUtc,
            newEndUtc: newEndUtc,
            reasonCode: "RESCHEDULE",
            reason: reason,
            initiator: initiator,
            changedByUserId: changedByUserId));

        // Reschedule active assignments to match new window
        foreach (var assignment in _assignments.Where(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.Dispatched))
        {
            assignment.Reschedule(newStartUtc, newEndUtc);
        }
    }

    public void Dispatch(Guid? changedByUserId = null)
    {
        if (Status == BookingStatus.Cancelled || Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot dispatch booking in status '{Status}'.");
        }

        var prevStatus = Status;
        Status = BookingStatus.Dispatched;
        DispatchStatus = BookingDispatchStatus.Dispatched;
        DispatchedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new BookingStatusHistory(
            tenantId: TenantId,
            bookingId: Id,
            fromStatus: prevStatus,
            toStatus: Status,
            reason: "Dispatched to field resources",
            trigger: "DispatcherAction",
            changedByUserId: changedByUserId));

        foreach (var assignment in _assignments.Where(a => a.Status == AssignmentStatus.Assigned))
        {
            assignment.Dispatch();
        }
    }

    public void StartProgress(Guid? changedByUserId = null)
    {
        if (Status == BookingStatus.Completed || Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot start progress for booking in status '{Status}'.");
        }

        var prevStatus = Status;
        Status = BookingStatus.InProgress;
        DispatchStatus = BookingDispatchStatus.OnSite;
        StartedAtUtc ??= DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new BookingStatusHistory(
            tenantId: TenantId,
            bookingId: Id,
            fromStatus: prevStatus,
            toStatus: Status,
            reason: "Field execution in progress",
            trigger: "FieldExecution",
            changedByUserId: changedByUserId));
    }

    public void Complete(Guid? changedByUserId = null)
    {
        if (Status == BookingStatus.Completed) return;
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled booking.");
        }

        var prevStatus = Status;
        Status = BookingStatus.Completed;
        DispatchStatus = BookingDispatchStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new BookingStatusHistory(
            tenantId: TenantId,
            bookingId: Id,
            fromStatus: prevStatus,
            toStatus: Status,
            reason: "All visit requirements completed",
            trigger: "ExecutionCompletion",
            changedByUserId: changedByUserId));
    }

    public void Cancel(string reason, Guid? changedByUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Trim().Length > 500)
        {
            throw new ArgumentException("Cancellation reason cannot exceed 500 characters.", nameof(reason));
        }
        if (Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel an already completed booking.");
        }
        if (Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }

        var prevStatus = Status;
        Status = BookingStatus.Cancelled;
        DispatchStatus = BookingDispatchStatus.Cancelled;
        CancellationReason = reason.Trim();
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new BookingStatusHistory(
            tenantId: TenantId,
            bookingId: Id,
            fromStatus: prevStatus,
            toStatus: Status,
            reason: reason,
            trigger: "Cancellation",
            changedByUserId: changedByUserId));

        foreach (var assignment in _assignments.Where(a => a.Status != AssignmentStatus.Completed && a.Status != AssignmentStatus.Cancelled))
        {
            assignment.Cancel(reason);
        }
    }
}

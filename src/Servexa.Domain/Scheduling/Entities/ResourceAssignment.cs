using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// One resource's participation in one Booking (LDM §9.8, Physical Model §12.6).
/// Independent lifecycle from Booking.
/// </summary>
public sealed class ResourceAssignment
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid ResourceId { get; private set; }
    public AssignmentRole AssignmentRole { get; private set; } = AssignmentRole.Lead;
    public DateTime PlannedStartUtc { get; private set; }
    public DateTime PlannedEndUtc { get; private set; }
    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Assigned;
    public string? SelectionRationale { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private ResourceAssignment() { }

    public ResourceAssignment(
        Guid tenantId,
        Guid bookingId,
        Guid resourceId,
        DateTime plannedStartUtc,
        DateTime plannedEndUtc,
        AssignmentRole assignmentRole = AssignmentRole.Lead,
        string? selectionRationale = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty.", nameof(bookingId));
        if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));
        if (plannedEndUtc <= plannedStartUtc) throw new ArgumentException("PlannedEndUtc must be strictly greater than PlannedStartUtc.", nameof(plannedEndUtc));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        BookingId = bookingId;
        ResourceId = resourceId;
        AssignmentRole = assignmentRole;
        PlannedStartUtc = plannedStartUtc;
        PlannedEndUtc = plannedEndUtc;
        Status = AssignmentStatus.Assigned;
        SelectionRationale = selectionRationale?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Dispatch()
    {
        if (Status != AssignmentStatus.Assigned)
        {
            throw new InvalidOperationException($"Cannot dispatch assignment in status '{Status}'. Expected '{AssignmentStatus.Assigned}'.");
        }
        Status = AssignmentStatus.Dispatched;
        DispatchedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void StartTravel()
    {
        if (Status != AssignmentStatus.Dispatched && Status != AssignmentStatus.Assigned)
        {
            throw new InvalidOperationException($"Cannot start travel from status '{Status}'.");
        }
        Status = AssignmentStatus.Traveling;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkArrived()
    {
        if (Status != AssignmentStatus.Traveling && Status != AssignmentStatus.Dispatched && Status != AssignmentStatus.Assigned)
        {
            throw new InvalidOperationException($"Cannot mark arrived from status '{Status}'.");
        }
        Status = AssignmentStatus.Arrived;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void StartWork()
    {
        if (Status != AssignmentStatus.Arrived && Status != AssignmentStatus.Traveling && Status != AssignmentStatus.Dispatched && Status != AssignmentStatus.Assigned)
        {
            throw new InvalidOperationException($"Cannot start work from status '{Status}'.");
        }
        Status = AssignmentStatus.InProgress;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != AssignmentStatus.InProgress && Status != AssignmentStatus.Arrived && Status != AssignmentStatus.Assigned && Status != AssignmentStatus.Dispatched)
        {
            throw new InvalidOperationException($"Cannot complete assignment in status '{Status}'.");
        }
        Status = AssignmentStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == AssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed assignment.");
        }
        Status = AssignmentStatus.Cancelled;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Replace(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == AssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot replace a completed assignment.");
        }
        Status = AssignmentStatus.Replaced;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newStartUtc, DateTime newEndUtc)
    {
        if (newEndUtc <= newStartUtc) throw new ArgumentException("New end time must be greater than new start time.", nameof(newEndUtc));
        PlannedStartUtc = newStartUtc;
        PlannedEndUtc = newEndUtc;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

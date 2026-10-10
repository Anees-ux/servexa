using Servexa.Domain.Field.Enums;

namespace Servexa.Domain.Field.Entities;

/// <summary>
/// Per-assignment technician execution session aggregate root (LDM §10.3, Physical Model §13.1).
/// Tracks individual technician travel, arrival, working intervals, and work completion.
/// </summary>
public sealed class ExecutionSession
{
    private readonly List<ExecutionInterval> _intervals = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ResourceAssignmentId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public ExecutionSessionStatus Status { get; private set; } = ExecutionSessionStatus.NotStarted;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? WorkSummary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ExecutionInterval> Intervals => _intervals.AsReadOnly();

    private ExecutionSession() { }

    public ExecutionSession(
        Guid tenantId,
        Guid resourceAssignmentId,
        Guid bookingId,
        Guid workOrderId,
        Guid resourceId,
        Guid userId,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (resourceAssignmentId == Guid.Empty) throw new ArgumentException("ResourceAssignmentId cannot be empty.", nameof(resourceAssignmentId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty.", nameof(bookingId));
        if (workOrderId == Guid.Empty) throw new ArgumentException("WorkOrderId cannot be empty.", nameof(workOrderId));
        if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ResourceAssignmentId = resourceAssignmentId;
        BookingId = bookingId;
        WorkOrderId = workOrderId;
        ResourceId = resourceId;
        UserId = userId;
        Status = ExecutionSessionStatus.NotStarted;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void StartTravel()
    {
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Traveling;
        _intervals.Add(new ExecutionInterval(TenantId, Id, ExecutionIntervalType.Travel, DateTime.UtcNow, "Traveling to job site"));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void ArriveOnSite()
    {
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Arrived;
        _intervals.Add(new ExecutionInterval(TenantId, Id, ExecutionIntervalType.OnSite, DateTime.UtcNow, "Arrived on site"));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void StartWork()
    {
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Working;
        StartedAtUtc ??= DateTime.UtcNow;
        _intervals.Add(new ExecutionInterval(TenantId, Id, ExecutionIntervalType.Work, DateTime.UtcNow, "Field work execution in progress"));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void PauseWork(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Paused;
        _intervals.Add(new ExecutionInterval(TenantId, Id, ExecutionIntervalType.Pause, DateTime.UtcNow, reason.Trim()));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void ResumeWork()
    {
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Working;
        _intervals.Add(new ExecutionInterval(TenantId, Id, ExecutionIntervalType.Work, DateTime.UtcNow, "Field work resumed"));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void EndSession(string workSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workSummary);
        CloseOpenInterval();
        Status = ExecutionSessionStatus.Ended;
        CompletedAtUtc = DateTime.UtcNow;
        WorkSummary = workSummary.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    private void CloseOpenInterval()
    {
        var open = _intervals.LastOrDefault(i => i.EndedAtUtc == null);
        open?.End();
    }
}

using Servexa.Domain.Field.Enums;

namespace Servexa.Domain.Field.Entities;

/// <summary>
/// Operational time interval recorded during technician execution (Physical Model §13.2, LDM §10.3).
/// </summary>
public sealed class ExecutionInterval
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ExecutionSessionId { get; private set; }
    public ExecutionIntervalType IntervalType { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    private ExecutionInterval() { }

    public ExecutionInterval(
        Guid tenantId,
        Guid executionSessionId,
        ExecutionIntervalType intervalType,
        DateTime startedAtUtc,
        string? reason = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (executionSessionId == Guid.Empty) throw new ArgumentException("ExecutionSessionId cannot be empty.", nameof(executionSessionId));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ExecutionSessionId = executionSessionId;
        IntervalType = intervalType;
        StartedAtUtc = startedAtUtc;
        Reason = reason?.Trim();
    }

    public void End(DateTime? endedAtUtc = null)
    {
        EndedAtUtc = endedAtUtc ?? DateTime.UtcNow;
    }
}

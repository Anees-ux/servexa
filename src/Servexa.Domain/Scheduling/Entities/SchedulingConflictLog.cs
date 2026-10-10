namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Audit record capturing detected scheduling conflict facts (LDM §9.11, Physical Model §12.9).
/// </summary>
public sealed class SchedulingConflictLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateTime AttemptedStartUtc { get; private set; }
    public DateTime AttemptedEndUtc { get; private set; }
    public Guid? BlockingCommitmentId { get; private set; }
    public string ConflictType { get; private set; } = "Overlap";
    public string? Reason { get; private set; }
    public Guid? AttemptedByUserId { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }

    private SchedulingConflictLog() { }

    public SchedulingConflictLog(
        Guid tenantId,
        Guid resourceId,
        DateTime attemptedStartUtc,
        DateTime attemptedEndUtc,
        Guid? blockingCommitmentId = null,
        string conflictType = "Overlap",
        string? reason = null,
        Guid? attemptedByUserId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ResourceId = resourceId;
        AttemptedStartUtc = attemptedStartUtc;
        AttemptedEndUtc = attemptedEndUtc;
        BlockingCommitmentId = blockingCommitmentId;
        ConflictType = string.IsNullOrWhiteSpace(conflictType) ? "Overlap" : conflictType.Trim();
        Reason = reason?.Trim();
        AttemptedByUserId = attemptedByUserId;
        AttemptedAtUtc = DateTime.UtcNow;
    }
}

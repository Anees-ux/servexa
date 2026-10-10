using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Committed time claim for an exclusive resource (LDM §9.9, Physical Model §12.7).
/// Invariant: For any one exclusive resource, no two Active commitments overlap in time.
/// </summary>
public sealed class ResourceCommitment
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid ResourceAssignmentId { get; private set; }
    public CommitmentKind CommitmentKind { get; private set; } = CommitmentKind.Direct;
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public CommitmentStatus Status { get; private set; } = CommitmentStatus.Active;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }
    public string? ReleaseReason { get; private set; }

    private ResourceCommitment() { }

    public ResourceCommitment(
        Guid tenantId,
        Guid resourceId,
        Guid resourceAssignmentId,
        DateTime startUtc,
        DateTime endUtc,
        CommitmentKind commitmentKind = CommitmentKind.Direct,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));
        if (resourceAssignmentId == Guid.Empty) throw new ArgumentException("ResourceAssignmentId cannot be empty.", nameof(resourceAssignmentId));
        if (endUtc <= startUtc) throw new ArgumentException("Commitment EndUtc must be strictly greater than StartUtc.", nameof(endUtc));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ResourceId = resourceId;
        ResourceAssignmentId = resourceAssignmentId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        CommitmentKind = commitmentKind;
        Status = CommitmentStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Release(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = CommitmentStatus.Released;
        ReleasedAtUtc = DateTime.UtcNow;
        ReleaseReason = reason.Trim();
    }

    public void Supersede(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = CommitmentStatus.Superseded;
        ReleasedAtUtc = DateTime.UtcNow;
        ReleaseReason = reason.Trim();
    }
}

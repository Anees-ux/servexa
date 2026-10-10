namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Technical concurrency guard table for R1 scheduling serialization (Physical Model §12.8).
/// Enforces deterministic row-level serialization for exclusive resource commitments within a short transaction.
/// </summary>
public sealed class ResourceScheduleGuard
{
    public Guid TenantId { get; private set; }
    public Guid ResourceId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTime TouchedAtUtc { get; private set; }

    private ResourceScheduleGuard() { }

    public ResourceScheduleGuard(Guid tenantId, Guid resourceId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));

        TenantId = tenantId;
        ResourceId = resourceId;
        TouchedAtUtc = DateTime.UtcNow;
    }

    public void Touch()
    {
        TouchedAtUtc = DateTime.UtcNow;
    }
}

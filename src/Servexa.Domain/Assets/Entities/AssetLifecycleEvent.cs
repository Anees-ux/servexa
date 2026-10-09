using Servexa.Domain.Assets.Enums;

namespace Servexa.Domain.Assets.Entities;

/// <summary>
/// Append-only lifecycle facts ledger recording state changes, movement, and ownership
/// transitions for an Asset (Logical Data Model §7.3 & §7.6; Physical Model §10.3).
/// </summary>
public class AssetLifecycleEvent
{
    // Parameterless constructor for EF Core instantiation
    private AssetLifecycleEvent()
    {
    }

    public AssetLifecycleEvent(
        Guid tenantId,
        Guid assetId,
        AssetLifecycleEventType eventType,
        AssetStatus? previousStatus = null,
        AssetStatus? newStatus = null,
        Guid? fromSiteId = null,
        Guid? toSiteId = null,
        Guid? fromOwnerAccountId = null,
        Guid? toOwnerAccountId = null,
        string? reason = null,
        Guid? actorUserId = null,
        DateTime? occurredAtUtc = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("AssetId cannot be empty.", nameof(assetId));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        AssetId = assetId;
        EventType = eventType;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        FromSiteId = fromSiteId;
        ToSiteId = toSiteId;
        FromOwnerAccountId = fromOwnerAccountId;
        ToOwnerAccountId = toOwnerAccountId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow;
        RecordedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public AssetLifecycleEventType EventType { get; private set; }
    public AssetStatus? PreviousStatus { get; private set; }
    public AssetStatus? NewStatus { get; private set; }
    public Guid? FromSiteId { get; private set; }
    public Guid? ToSiteId { get; private set; }
    public Guid? FromOwnerAccountId { get; private set; }
    public Guid? ToOwnerAccountId { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
}

using Servexa.Domain.Assets.Enums;

namespace Servexa.Domain.Assets.Entities;

/// <summary>
/// Asset represents a specific physical equipment instance with a stable identity
/// across site moves and ownership changes.
/// </summary>
public class Asset
{
    private readonly List<AssetLifecycleEvent> _lifecycleEvents = [];

    // Parameterless constructor for EF Core instantiation
    private Asset()
    {
    }

    public Asset(
        Guid tenantId,
        string assetNumber,
        Guid equipmentModelId,
        string? serialNumber = null,
        Guid? currentSiteId = null,
        Guid? currentOwnerAccountId = null,
        AssetStatus status = AssetStatus.Active,
        DateTime? installedAtUtc = null,
        Guid? id = null,
        Guid? actorUserId = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (equipmentModelId == Guid.Empty)
        {
            throw new ArgumentException("EquipmentModelId cannot be empty.", nameof(equipmentModelId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(assetNumber);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        AssetNumber = assetNumber.Trim().ToUpperInvariant();
        EquipmentModelId = equipmentModelId;
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        CurrentSiteId = currentSiteId;
        CurrentOwnerAccountId = currentOwnerAccountId;
        Status = status;
        InstalledAtUtc = installedAtUtc ?? (status == AssetStatus.Active ? DateTime.UtcNow : null);
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;

        // Log initial registration in append-only lifecycle ledger
        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Registered,
            previousStatus: null,
            newStatus: Status,
            fromSiteId: null,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: null,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: "Initial asset registration",
            actorUserId: actorUserId,
            occurredAtUtc: CreatedAtUtc));
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string AssetNumber { get; private set; } = null!;
    public Guid EquipmentModelId { get; private set; }
    public string? SerialNumber { get; private set; }
    public Guid? CurrentSiteId { get; private set; }
    public Guid? CurrentOwnerAccountId { get; private set; }
    public AssetStatus Status { get; private set; }
    public DateTime? InstalledAtUtc { get; private set; }
    public DateTime? DecommissionedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public IReadOnlyCollection<AssetLifecycleEvent> LifecycleEvents => _lifecycleEvents.AsReadOnly();

    public void MoveToSite(Guid siteId, string? reason = null, Guid? actorUserId = null)
    {
        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Target SiteId cannot be empty.", nameof(siteId));
        }

        var previousSiteId = CurrentSiteId;
        CurrentSiteId = siteId;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Moved,
            previousStatus: Status,
            newStatus: Status,
            fromSiteId: previousSiteId,
            toSiteId: siteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Relocated to site",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void AssignOwnerAccount(Guid? accountId, string? reason = null, Guid? actorUserId = null)
    {
        var previousOwnerId = CurrentOwnerAccountId;
        CurrentOwnerAccountId = accountId;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.OwnershipChanged,
            previousStatus: Status,
            newStatus: Status,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: previousOwnerId,
            toOwnerAccountId: accountId,
            reason: reason ?? "Assigned customer owner account",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void Commission(DateTime? installedAtUtc = null, string? reason = null, Guid? actorUserId = null)
    {
        if (Status == AssetStatus.Decommissioned || Status == AssetStatus.Replaced)
        {
            throw new InvalidOperationException($"Cannot commission an asset in terminal status '{Status}'.");
        }

        var previousStatus = Status;
        Status = AssetStatus.Active;
        InstalledAtUtc = installedAtUtc ?? DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Commissioned,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Active,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset commissioned into active service",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void MarkDegraded(string? reason = null, Guid? actorUserId = null)
    {
        if (Status != AssetStatus.Active)
        {
            throw new InvalidOperationException($"Only Active assets can transition to Degraded. Current status: '{Status}'.");
        }

        var previousStatus = Status;
        Status = AssetStatus.Degraded;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Degraded,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Degraded,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset marked degraded / impaired",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void RestoreActive(string? reason = null, Guid? actorUserId = null)
    {
        if (Status != AssetStatus.Degraded)
        {
            throw new InvalidOperationException($"Only Degraded assets can be restored to Active directly. Current status: '{Status}'.");
        }

        var previousStatus = Status;
        Status = AssetStatus.Active;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Restored,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Active,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset restored to active operational service",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void Decommission(string? reason = null, Guid? actorUserId = null, DateTime? decommissionedAtUtc = null)
    {
        if (Status == AssetStatus.Decommissioned || Status == AssetStatus.Replaced)
        {
            throw new InvalidOperationException("Asset is already decommissioned or replaced.");
        }

        var previousStatus = Status;
        Status = AssetStatus.Decommissioned;
        DecommissionedAtUtc = decommissionedAtUtc ?? DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Decommissioned,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Decommissioned,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset decommissioned from service",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void Reactivate(string? reason = null, Guid? actorUserId = null)
    {
        if (Status != AssetStatus.Decommissioned)
        {
            throw new InvalidOperationException($"Only Decommissioned assets can be reactivated. Current status: '{Status}'.");
        }

        var previousStatus = Status;
        Status = AssetStatus.Active;
        DecommissionedAtUtc = null;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Reactivated,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Active,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset reactivated into operational service",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }

    public void Replace(string? reason = null, Guid? actorUserId = null)
    {
        var previousStatus = Status;
        Status = AssetStatus.Replaced;
        DecommissionedAtUtc ??= DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;

        _lifecycleEvents.Add(new AssetLifecycleEvent(
            tenantId: TenantId,
            assetId: Id,
            eventType: AssetLifecycleEventType.Replaced,
            previousStatus: previousStatus,
            newStatus: AssetStatus.Replaced,
            fromSiteId: CurrentSiteId,
            toSiteId: CurrentSiteId,
            fromOwnerAccountId: CurrentOwnerAccountId,
            toOwnerAccountId: CurrentOwnerAccountId,
            reason: reason ?? "Asset replaced by new equipment",
            actorUserId: actorUserId,
            occurredAtUtc: ModifiedAtUtc));
    }
}

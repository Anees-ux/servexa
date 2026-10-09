namespace Servexa.Domain.Assets.Enums;

/// <summary>
/// Event types recorded in the append-only AssetLifecycleEvent ledger (LDM §7.6).
/// </summary>
public enum AssetLifecycleEventType : short
{
    Registered = 1,
    Commissioned = 2,
    Moved = 3,
    Degraded = 4,
    Restored = 5,
    Decommissioned = 6,
    Reactivated = 7,
    Replaced = 8,
    OwnershipChanged = 9
}

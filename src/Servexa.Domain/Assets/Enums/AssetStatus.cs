namespace Servexa.Domain.Assets.Enums;

/// <summary>
/// Lifecycle status of an Asset physical unit.
/// Stored as smallint in physical persistence.
/// </summary>
public enum AssetStatus : short
{
    PreInstallation = 1,
    Active = 2,
    Degraded = 3,
    Decommissioned = 4,
    Replaced = 5
}

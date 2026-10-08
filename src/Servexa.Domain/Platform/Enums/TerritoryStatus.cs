namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for a geographic / service-coverage Territory.
/// Stored as smallint in physical persistence.
/// </summary>
public enum TerritoryStatus : short
{
    Active = 1,
    Inactive = 2
}

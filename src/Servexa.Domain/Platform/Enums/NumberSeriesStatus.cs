namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for a business number series generator.
/// Stored as smallint in physical persistence.
/// </summary>
public enum NumberSeriesStatus : short
{
    Active = 1,
    Inactive = 2
}

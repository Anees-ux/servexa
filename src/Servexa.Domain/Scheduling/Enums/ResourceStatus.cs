namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Operational status of a schedulable resource (Physical Model §12.1).
/// </summary>
public enum ResourceStatus : short
{
    Active = 1,
    Inactive = 2
}

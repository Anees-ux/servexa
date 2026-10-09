namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Operational priority for a WorkOrder.
/// Stored as smallint in physical persistence.
/// </summary>
public enum WorkOrderPriority : short
{
    Low = 1,
    Standard = 2,
    High = 3,
    Critical = 4
}

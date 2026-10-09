namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Operational status state machine for a WorkOrder.
/// Stored as smallint in physical persistence.
/// </summary>
public enum WorkOrderOperationalStatus : short
{
    Draft = 1,
    Approved = 2,
    Scheduled = 3,
    InProgress = 4,
    Paused = 5,
    OperationallyComplete = 6,
    Cancelled = 7
}

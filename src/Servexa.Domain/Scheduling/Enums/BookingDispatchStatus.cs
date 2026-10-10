namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Dispatch status rollup for a Booking envelope (Physical Model §12.5).
/// </summary>
public enum BookingDispatchStatus : short
{
    Unassigned = 1,
    Assigned = 2,
    Dispatched = 3,
    Acknowledged = 4,
    EnRoute = 5,
    OnSite = 6,
    Completed = 7,
    Cancelled = 8
}

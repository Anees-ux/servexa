namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Status of one resource's participation in a Booking (LDM §9.8, Physical Model §12.6).
/// Lifecycle: Assigned -> Dispatched -> Traveling -> Arrived -> InProgress -> Completed (or Cancelled / Replaced / NoShow).
/// </summary>
public enum AssignmentStatus : short
{
    Assigned = 1,
    Dispatched = 2,
    Traveling = 3,
    Arrived = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
    Replaced = 8,
    NoShow = 9
}

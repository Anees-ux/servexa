namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Authoritative operational lifecycle state for a customer visit Booking (LDM §9.6).
/// Lifecycle: Proposed -> Scheduled -> Confirmed -> Dispatched -> InProgress -> Completed (or Cancelled / NoAccess).
/// </summary>
public enum BookingStatus : short
{
    Proposed = 1,
    Scheduled = 2,
    Confirmed = 3,
    Dispatched = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
    NoAccess = 8
}

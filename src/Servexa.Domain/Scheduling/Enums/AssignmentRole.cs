namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Operational role of a resource participating in a Booking (LDM §9.8, Physical Model §12.6).
/// Invariant: At most one active Lead per booking where work type requires a lead.
/// </summary>
public enum AssignmentRole : short
{
    Lead = 1,
    Support = 2,
    Equipment = 3,
    Observer = 4
}

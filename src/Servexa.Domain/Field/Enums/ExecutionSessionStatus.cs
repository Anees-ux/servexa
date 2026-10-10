namespace Servexa.Domain.Field.Enums;

/// <summary>
/// Status of an individual technician execution session (LDM §10.3, Physical Model §13.1).
/// Lifecycle: NotStarted -> Traveling -> Arrived -> Working (or Paused) -> Ended.
/// </summary>
public enum ExecutionSessionStatus : short
{
    NotStarted = 1,
    Traveling = 2,
    Arrived = 3,
    Working = 4,
    Paused = 5,
    Ended = 6
}

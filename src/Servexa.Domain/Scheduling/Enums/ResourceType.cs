namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Schedulable resource categorization (Physical Model §12.1, LDM §9.1).
/// </summary>
public enum ResourceType : short
{
    Technician = 1,
    Subcontractor = 2,
    Crew = 3,
    Equipment = 4
}

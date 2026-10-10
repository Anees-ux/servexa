namespace Servexa.Domain.Field.Enums;

/// <summary>
/// Operational interval category recorded during execution (LDM §10.3, Physical Model §13.2).
/// </summary>
public enum ExecutionIntervalType : short
{
    Travel = 1,
    OnSite = 2,
    Work = 3,
    Pause = 4
}

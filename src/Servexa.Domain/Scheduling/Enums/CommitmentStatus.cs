namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Status of a committed time claim for a resource (LDM §9.9, Physical Model §12.7).
/// </summary>
public enum CommitmentStatus : short
{
    Active = 1,
    Released = 2,
    Superseded = 3
}

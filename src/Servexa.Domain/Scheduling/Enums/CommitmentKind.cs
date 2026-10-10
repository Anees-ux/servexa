namespace Servexa.Domain.Scheduling.Enums;

/// <summary>
/// Origin kind of resource commitment (LDM §9.9).
/// </summary>
public enum CommitmentKind : short
{
    Direct = 1,
    CrewMember = 2
}

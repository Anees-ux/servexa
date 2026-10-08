namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Compliance retention state for a file object.
/// Stored as smallint in physical persistence.
/// </summary>
public enum FileRetentionState : short
{
    Active = 1,
    LegalHold = 2,
    PendingPurge = 3
}

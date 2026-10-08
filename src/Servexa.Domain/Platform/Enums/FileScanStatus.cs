namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Antivirus and malware scan status for a binary file object.
/// Stored as smallint in physical persistence.
/// </summary>
public enum FileScanStatus : short
{
    PendingScan = 1,
    Available = 2,
    Rejected = 3
}

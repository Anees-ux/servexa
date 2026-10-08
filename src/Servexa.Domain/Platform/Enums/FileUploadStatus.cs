namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Upload progression status for a binary file object.
/// Stored as smallint in physical persistence.
/// </summary>
public enum FileUploadStatus : short
{
    Initiated = 1,
    Uploaded = 2,
    Failed = 3
}

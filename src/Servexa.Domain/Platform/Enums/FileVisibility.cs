namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Visibility classification for a file object (internal vs customer visible).
/// Stored as smallint in physical persistence.
/// </summary>
public enum FileVisibility : short
{
    Internal = 1,
    CustomerVisible = 2
}

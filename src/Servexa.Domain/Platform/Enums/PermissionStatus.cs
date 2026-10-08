namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Status for a global Permission catalogue item.
/// Stored as smallint in physical persistence.
/// </summary>
public enum PermissionStatus : short
{
    Active = 1,
    Inactive = 2
}

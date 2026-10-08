namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for an authorization Role.
/// Stored as smallint in physical persistence.
/// </summary>
public enum RoleStatus : short
{
    Active = 1,
    Inactive = 2
}

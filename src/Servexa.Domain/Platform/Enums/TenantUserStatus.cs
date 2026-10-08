namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for a TenantUser (tenant-scoped membership and identity link).
/// Stored as smallint in physical persistence.
/// </summary>
public enum TenantUserStatus : short
{
    Invited = 1,
    Active = 2,
    Disabled = 3
}

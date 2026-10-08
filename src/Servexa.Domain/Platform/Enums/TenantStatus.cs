namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for a Tenant.
/// Stored as smallint in physical persistence.
/// </summary>
public enum TenantStatus : short
{
    Provisioning = 1,
    Active = 2,
    Suspended = 3,
    Deactivated = 4,
    Purged = 5
}

namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Contextual scope type for authorization grants.
/// Stored as smallint in physical persistence.
/// </summary>
public enum ScopeType : short
{
    Tenant = 1,
    Branch = 2,
    Territory = 3,
    Account = 4,
    Site = 5
}

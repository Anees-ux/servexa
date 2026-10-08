namespace Servexa.Domain.Platform.Enums;

/// <summary>
/// Lifecycle status for an organizational Branch.
/// Stored as smallint in physical persistence.
/// </summary>
public enum BranchStatus : short
{
    Active = 1,
    Inactive = 2
}

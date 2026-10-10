namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Execution status of a WorkOrderScopeItem.
/// </summary>
public enum WorkOrderScopeItemStatus : short
{
    Authorized = 1,
    Fulfilled = 2,
    NotRequired = 3,
    Cancelled = 4
}

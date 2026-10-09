namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Role of an Asset within a WorkOrder.
/// Stored as smallint in physical persistence.
/// </summary>
public enum WorkOrderAssetRole : short
{
    Primary = 1,
    Included = 2
}

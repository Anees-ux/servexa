namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Status of an Asset scope item within a WorkOrder.
/// Stored as smallint in physical persistence.
/// </summary>
public enum WorkOrderAssetStatus : short
{
    InScope = 1,
    Removed = 2,
    Completed = 3
}

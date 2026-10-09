using Servexa.Domain.Service.Enums;

namespace Servexa.Domain.Service.Entities;

/// <summary>
/// WorkOrderAsset links an Asset to a WorkOrder with an operational role.
/// All attached assets must share the WorkOrder site unless multi-site exception is authorized.
/// </summary>
public class WorkOrderAsset
{
    // Parameterless constructor for EF Core instantiation
    private WorkOrderAsset()
    {
    }

    public WorkOrderAsset(
        Guid tenantId,
        Guid workOrderId,
        Guid assetId,
        Guid siteIdAtTime,
        WorkOrderAssetRole role = WorkOrderAssetRole.Primary,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (workOrderId == Guid.Empty)
        {
            throw new ArgumentException("WorkOrderId cannot be empty.", nameof(workOrderId));
        }

        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("AssetId cannot be empty.", nameof(assetId));
        }

        if (siteIdAtTime == Guid.Empty)
        {
            throw new ArgumentException("SiteIdAtTime cannot be empty.", nameof(siteIdAtTime));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderId = workOrderId;
        AssetId = assetId;
        SiteIdAtTime = siteIdAtTime;
        Role = role;
        Status = WorkOrderAssetStatus.InScope;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid AssetId { get; private set; }
    public WorkOrderAssetRole Role { get; private set; }
    public Guid SiteIdAtTime { get; private set; }
    public WorkOrderAssetStatus Status { get; private set; }

    public void MarkCompleted()
    {
        Status = WorkOrderAssetStatus.Completed;
    }

    public void MarkRemoved()
    {
        Status = WorkOrderAssetStatus.Removed;
    }
}

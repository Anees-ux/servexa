using Servexa.Domain.Service.Enums;

namespace Servexa.Domain.Service.Entities;

/// <summary>
/// Represents an authorized operational scope item within a Work Order (Physical Model §11.5).
/// Serves as the primary unit of authorized scope evaluated by POL-01.
/// </summary>
public class WorkOrderScopeItem
{
    private WorkOrderScopeItem() { }

    public WorkOrderScopeItem(
        Guid tenantId,
        Guid workOrderId,
        int sequence,
        WorkOrderScopeType scopeType,
        string description,
        bool isRequiredForCompletion = true,
        Guid? assetId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (workOrderId == Guid.Empty)
            throw new ArgumentException("WorkOrderId is required.", nameof(workOrderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (description.Trim().Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderId = workOrderId;
        Sequence = sequence > 0 ? sequence : 1;
        ScopeType = scopeType;
        Description = description.Trim();
        Status = WorkOrderScopeItemStatus.Authorized;
        IsRequiredForCompletion = isRequiredForCompletion;
        AssetId = assetId;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public int Sequence { get; private set; }
    public WorkOrderScopeType ScopeType { get; private set; }
    public string Description { get; private set; } = null!;
    public WorkOrderScopeItemStatus Status { get; private set; }
    public bool IsRequiredForCompletion { get; private set; }
    public Guid? AssetId { get; private set; }
    public DateTime? FulfilledAtUtc { get; private set; }
    public Guid? FulfilledByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }

    public void Fulfill(Guid userId, DateTime? fulfilledAtUtc = null)
    {
        if (Status == WorkOrderScopeItemStatus.Cancelled)
            throw new InvalidOperationException("Cannot fulfill a cancelled scope item.");

        Status = WorkOrderScopeItemStatus.Fulfilled;
        FulfilledByUserId = userId;
        FulfilledAtUtc = fulfilledAtUtc ?? DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void MarkNotRequired(Guid userId)
    {
        if (Status == WorkOrderScopeItemStatus.Fulfilled)
            throw new InvalidOperationException("Cannot mark an already fulfilled scope item as not required.");

        Status = WorkOrderScopeItemStatus.NotRequired;
        FulfilledByUserId = userId;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(Guid userId)
    {
        if (Status == WorkOrderScopeItemStatus.Fulfilled)
            throw new InvalidOperationException("Cannot cancel an already fulfilled scope item.");

        Status = WorkOrderScopeItemStatus.Cancelled;
        FulfilledByUserId = userId;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

using Servexa.Domain.Service.Enums;

namespace Servexa.Domain.Service.Entities;

/// <summary>
/// Append-only ledger recording operational state machine transitions of a WorkOrder.
/// </summary>
public class WorkOrderStatusHistory
{
    // Parameterless constructor for EF Core instantiation
    private WorkOrderStatusHistory()
    {
    }

    public WorkOrderStatusHistory(
        Guid tenantId,
        Guid workOrderId,
        WorkOrderOperationalStatus fromStatus,
        WorkOrderOperationalStatus toStatus,
        string? pauseReasonCode = null,
        Guid? changedByUserId = null,
        string? reason = null,
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

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderId = workOrderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        PauseReasonCode = string.IsNullOrWhiteSpace(pauseReasonCode) ? null : pauseReasonCode.Trim();
        ChangedByUserId = changedByUserId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ChangedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public WorkOrderOperationalStatus FromStatus { get; private set; }
    public WorkOrderOperationalStatus ToStatus { get; private set; }
    public string? PauseReasonCode { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }
}

using Servexa.Domain.Service.Enums;

namespace Servexa.Domain.Service.Entities;

/// <summary>
/// WorkOrder is the aggregate root owning authorized work scope and operational lifecycle.
/// Follows the approved operational lifecycle:
/// Draft -> Approved -> Scheduled -> InProgress <-> Paused -> OperationallyComplete
/// Any active state -> Cancelled
/// OperationallyComplete -> Approved (Reopen with explicit logged reason)
/// </summary>
public class WorkOrder
{
    private readonly List<WorkOrderAsset> _assets = [];
    private readonly List<WorkOrderStatusHistory> _statusHistory = [];

    // Parameterless constructor for EF Core instantiation
    private WorkOrder()
    {
    }

    public WorkOrder(
        Guid tenantId,
        string workOrderNumber,
        Guid serviceAccountId,
        Guid billToAccountId,
        Guid primarySiteId,
        string workTypeCode,
        string summary,
        string billToSnapshotJson,
        WorkOrderPriority priority = WorkOrderPriority.Standard,
        string? description = null,
        Guid? serviceRequestId = null,
        Guid? changedByUserId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (serviceAccountId == Guid.Empty)
        {
            throw new ArgumentException("ServiceAccountId cannot be empty.", nameof(serviceAccountId));
        }

        if (billToAccountId == Guid.Empty)
        {
            throw new ArgumentException("BillToAccountId cannot be empty.", nameof(billToAccountId));
        }

        if (primarySiteId == Guid.Empty)
        {
            throw new ArgumentException("PrimarySiteId cannot be empty.", nameof(primarySiteId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(workOrderNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(workTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(billToSnapshotJson);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderNumber = workOrderNumber.Trim().ToUpperInvariant();
        ServiceAccountId = serviceAccountId;
        BillToAccountId = billToAccountId;
        PrimarySiteId = primarySiteId;
        WorkTypeCode = workTypeCode.Trim().ToUpperInvariant();
        Summary = summary.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        BillToSnapshotJson = billToSnapshotJson.Trim();
        Priority = priority;
        OperationalStatus = WorkOrderOperationalStatus.Draft;
        ServiceRequestId = serviceRequestId;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;

        // Log initial draft creation
        _statusHistory.Add(new WorkOrderStatusHistory(
            tenantId: TenantId,
            workOrderId: Id,
            fromStatus: WorkOrderOperationalStatus.Draft,
            toStatus: WorkOrderOperationalStatus.Draft,
            changedByUserId: changedByUserId,
            reason: "Initial creation as Draft"));
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string WorkOrderNumber { get; private set; } = null!;
    public Guid? ServiceRequestId { get; private set; }
    public Guid ServiceAccountId { get; private set; }
    public Guid BillToAccountId { get; private set; }
    public Guid PrimarySiteId { get; private set; }
    public string WorkTypeCode { get; private set; } = null!;
    public WorkOrderPriority Priority { get; private set; }
    public WorkOrderOperationalStatus OperationalStatus { get; private set; }
    public string Summary { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? PauseReasonCode { get; private set; }
    public string? PauseNote { get; private set; }
    public string BillToSnapshotJson { get; private set; } = null!;
    public DateTime? OperationallyCompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public IReadOnlyCollection<WorkOrderAsset> Assets => _assets.AsReadOnly();
    public IReadOnlyCollection<WorkOrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void AttachAsset(Guid assetId, Guid siteIdAtTime, WorkOrderAssetRole role = WorkOrderAssetRole.Primary)
    {
        if (OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete || OperationalStatus == WorkOrderOperationalStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot attach assets when work order is '{OperationalStatus}'.");
        }

        if (_assets.Any(a => a.AssetId == assetId && a.Status == WorkOrderAssetStatus.InScope))
        {
            throw new InvalidOperationException($"Asset '{assetId}' is already attached to this work order.");
        }

        _assets.Add(new WorkOrderAsset(TenantId, Id, assetId, siteIdAtTime, role));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string summary, string? description, WorkOrderPriority priority, string workTypeCode)
    {
        if (OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete || OperationalStatus == WorkOrderOperationalStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot modify details when work order is '{OperationalStatus}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(workTypeCode);

        Summary = summary.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        WorkTypeCode = workTypeCode.Trim().ToUpperInvariant();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Approve(Guid? changedByUserId = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.Draft)
        {
            throw new InvalidOperationException($"Only Draft work orders can be approved. Current status: '{OperationalStatus}'.");
        }

        RecordTransition(WorkOrderOperationalStatus.Approved, changedByUserId, reason: "Work order approved for execution");
    }

    public void MarkScheduled(Guid? changedByUserId = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.Approved)
        {
            throw new InvalidOperationException($"Work order must be Approved to be Scheduled. Current status: '{OperationalStatus}'.");
        }

        RecordTransition(WorkOrderOperationalStatus.Scheduled, changedByUserId, reason: "Technician / booking scheduled");
    }

    public void StartProgress(Guid? changedByUserId = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.Scheduled &&
            OperationalStatus != WorkOrderOperationalStatus.Paused &&
            OperationalStatus != WorkOrderOperationalStatus.Approved)
        {
            throw new InvalidOperationException($"Work order cannot start from status '{OperationalStatus}'. Expected Scheduled, Approved, or Paused.");
        }

        PauseReasonCode = null;
        PauseNote = null;
        RecordTransition(WorkOrderOperationalStatus.InProgress, changedByUserId, reason: "Technician work in progress");
    }

    public void Pause(string reasonCode, string? note = null, Guid? changedByUserId = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.InProgress)
        {
            throw new InvalidOperationException($"Only work orders in progress can be paused. Current status: '{OperationalStatus}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        PauseReasonCode = reasonCode.Trim().ToUpperInvariant();
        PauseNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        RecordTransition(WorkOrderOperationalStatus.Paused, changedByUserId, pauseReasonCode: PauseReasonCode, reason: $"Work paused: {PauseReasonCode} ({PauseNote})");
    }

    public void Complete(Guid? changedByUserId = null, DateTime? completedAtUtc = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.InProgress && OperationalStatus != WorkOrderOperationalStatus.Paused)
        {
            throw new InvalidOperationException($"Only InProgress or Paused work orders can be completed. Current status: '{OperationalStatus}'.");
        }

        OperationallyCompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        PauseReasonCode = null;
        PauseNote = null;

        RecordTransition(WorkOrderOperationalStatus.OperationallyComplete, changedByUserId, reason: "Work operationally completed");
    }

    public void Reopen(string reason, Guid? changedByUserId = null)
    {
        if (OperationalStatus != WorkOrderOperationalStatus.OperationallyComplete)
        {
            throw new InvalidOperationException($"Only OperationallyComplete work orders can be reopened. Current status: '{OperationalStatus}'.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        OperationallyCompletedAtUtc = null;
        RecordTransition(WorkOrderOperationalStatus.Approved, changedByUserId, reason: reason.Trim());
    }

    public void Cancel(string reason, Guid? changedByUserId = null)
    {
        if (OperationalStatus == WorkOrderOperationalStatus.Cancelled)
        {
            throw new InvalidOperationException("Work order is already cancelled.");
        }

        if (OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete)
        {
            throw new InvalidOperationException("Operationally complete work orders cannot be cancelled; reopen or issue callback instead.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RecordTransition(WorkOrderOperationalStatus.Cancelled, changedByUserId, reason: reason.Trim());
    }

    private void RecordTransition(
        WorkOrderOperationalStatus toStatus,
        Guid? changedByUserId,
        string? pauseReasonCode = null,
        string? reason = null)
    {
        var fromStatus = OperationalStatus;
        OperationalStatus = toStatus;
        ModifiedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new WorkOrderStatusHistory(
            tenantId: TenantId,
            workOrderId: Id,
            fromStatus: fromStatus,
            toStatus: toStatus,
            pauseReasonCode: pauseReasonCode,
            changedByUserId: changedByUserId,
            reason: reason));
    }
}

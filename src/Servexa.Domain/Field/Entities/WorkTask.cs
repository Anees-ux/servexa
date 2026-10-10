using Servexa.Domain.Field.Enums;

namespace Servexa.Domain.Field.Entities;

/// <summary>
/// WorkTask represents a discrete task, checklist item, or inspection task instance
/// assigned to a Work Order, with optional asset attribution (FIE-011) and completion gating (FIE-003).
/// Physical Data Model: §13.3 field.WorkTasks
/// Logical Data Model: §10.4 WorkTask
/// </summary>
public class WorkTask
{
    // Parameterless constructor for EF Core instantiation
    private WorkTask()
    {
    }

    public WorkTask(
        Guid tenantId,
        Guid workOrderId,
        int sequence,
        string title,
        WorkTaskType taskType = WorkTaskType.Standard,
        bool isRequired = true,
        WorkTaskGate gate = WorkTaskGate.WorkOrderCompletion,
        string? description = null,
        Guid? assetId = null,
        Guid? assignmentId = null,
        Guid? bookingId = null,
        Guid? workOrderScopeItemId = null,
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

        if (sequence < 0)
        {
            throw new ArgumentException("Sequence cannot be negative.", nameof(sequence));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderId = workOrderId;
        Sequence = sequence;
        Title = title.Trim();
        TaskType = taskType;
        IsRequired = isRequired;
        Gate = gate;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AssetId = assetId;
        AssignmentId = assignmentId;
        BookingId = bookingId;
        WorkOrderScopeItemId = workOrderScopeItemId;
        Status = WorkTaskStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid? WorkOrderScopeItemId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? AssignmentId { get; private set; }
    public Guid? AssetId { get; private set; }
    public int Sequence { get; private set; }
    public WorkTaskType TaskType { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public WorkTaskStatus Status { get; private set; }
    public bool IsRequired { get; private set; }
    public WorkTaskGate Gate { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public string? SkipReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void Start(Guid userId)
    {
        if (Status == WorkTaskStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot start already completed task '{Title}'.");
        }

        if (Status == WorkTaskStatus.Skipped)
        {
            throw new InvalidOperationException($"Cannot start skipped task '{Title}'.");
        }

        Status = WorkTaskStatus.InProgress;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Complete(Guid userId, DateTime? completedAtUtc = null)
    {
        if (Status == WorkTaskStatus.Completed)
        {
            return; // Idempotent
        }

        Status = WorkTaskStatus.Completed;
        CompletedByUserId = userId;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        SkipReason = null;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Skip(string reason, Guid userId)
    {
        if (IsRequired && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException($"A non-empty skip reason is required to skip required task '{Title}'.");
        }

        Status = WorkTaskStatus.Skipped;
        CompletedByUserId = userId;
        CompletedAtUtc = DateTime.UtcNow;
        SkipReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Reset(Guid userId)
    {
        Status = WorkTaskStatus.Pending;
        CompletedByUserId = null;
        CompletedAtUtc = null;
        SkipReason = null;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void AssignToResource(Guid assignmentId, Guid? bookingId = null)
    {
        AssignmentId = assignmentId;
        if (bookingId.HasValue)
        {
            BookingId = bookingId;
        }
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void AttributeAsset(Guid assetId)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("AssetId cannot be empty.", nameof(assetId));
        }

        AssetId = assetId;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

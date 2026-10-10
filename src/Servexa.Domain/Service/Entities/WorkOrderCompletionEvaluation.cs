using Servexa.Domain.Service.Enums;

namespace Servexa.Domain.Service.Entities;

/// <summary>
/// Append-only evaluation ledger record for Work Order operational completion (Physical Model §11.7).
/// Captures the authoritative evaluation snapshot of all completion gates, reasons, and outcome.
/// </summary>
public class WorkOrderCompletionEvaluation
{
    private WorkOrderCompletionEvaluation() { }

    public WorkOrderCompletionEvaluation(
        Guid tenantId,
        Guid workOrderId,
        Guid commandId,
        WorkOrderCompletionOutcome outcome,
        string gateResultsJson,
        Guid? evaluatedByUserId = null,
        Guid? triggerBookingId = null,
        string? policySnapshotJson = null,
        string? summary = null,
        string? notes = null,
        DateTime? evaluatedAtUtc = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (workOrderId == Guid.Empty)
            throw new ArgumentException("WorkOrderId is required.", nameof(workOrderId));
        if (commandId == Guid.Empty)
            throw new ArgumentException("CommandId is required for idempotent tracking.", nameof(commandId));
        ArgumentException.ThrowIfNullOrWhiteSpace(gateResultsJson);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        WorkOrderId = workOrderId;
        CommandId = commandId;
        Outcome = outcome;
        GateResultsJson = gateResultsJson.Trim();
        EvaluatedByUserId = evaluatedByUserId;
        TriggerBookingId = triggerBookingId;
        PolicySnapshotJson = string.IsNullOrWhiteSpace(policySnapshotJson) ? null : policySnapshotJson.Trim();
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        EvaluatedAtUtc = evaluatedAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid CommandId { get; private set; }
    public DateTime EvaluatedAtUtc { get; private set; }
    public Guid? EvaluatedByUserId { get; private set; }
    public WorkOrderCompletionOutcome Outcome { get; private set; }
    public string GateResultsJson { get; private set; } = null!;
    public string? PolicySnapshotJson { get; private set; }
    public Guid? TriggerBookingId { get; private set; }
    public string? Summary { get; private set; }
    public string? Notes { get; private set; }

    public bool IsEligibleForCompletion => Outcome == WorkOrderCompletionOutcome.OperationallyComplete;
}

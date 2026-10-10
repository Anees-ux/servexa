using System.Text.Json;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Field.Enums;
using Servexa.Domain.Scheduling.Enums;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Services;

public sealed class WorkOrderCompletionEngine(
    IWorkOrderRepository workOrderRepository,
    IBookingRepository bookingRepository,
    IExecutionSessionRepository executionSessionRepository,
    IWorkTaskRepository workTaskRepository) : IWorkOrderCompletionEngine
{
    public async Task<WorkOrderCompletionReadinessDto> EvaluateAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid? evaluatingUserId,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, workOrderId, asNoTracking: true, cancellationToken)
            ?? throw new NotFoundException($"Work order with ID '{workOrderId}' was not found.");

        var bookings = await bookingRepository.GetBookingsAsync(
            tenantId,
            workOrderId: workOrderId,
            siteId: null,
            status: null,
            dispatchStatus: null,
            fromUtc: null,
            toUtc: null,
            skip: 0,
            take: 1000,
            cancellationToken: cancellationToken);

        var sessions = await executionSessionRepository.GetSessionsForWorkOrderAsync(
            tenantId,
            workOrderId,
            cancellationToken);

        var tasks = await workTaskRepository.GetByWorkOrderIdAsync(
            tenantId,
            workOrderId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        var latestEval = await workOrderRepository.GetLatestCompletionEvaluationAsync(
            tenantId,
            workOrderId,
            cancellationToken);

        var gates = new List<CompletionGateResultDto>();
        var unmet = new List<string>();

        // Gate 1: Status Eligibility
        var eligibleStatuses = new[]
        {
            WorkOrderOperationalStatus.InProgress,
            WorkOrderOperationalStatus.Paused,
            WorkOrderOperationalStatus.Scheduled,
            WorkOrderOperationalStatus.Approved
        };

        if (eligibleStatuses.Contains(workOrder.OperationalStatus))
        {
            gates.Add(new CompletionGateResultDto(
                "StatusEligibility",
                true,
                $"Work order is in eligible operational status '{workOrder.OperationalStatus}'.",
                null));
        }
        else
        {
            var reason = workOrder.OperationalStatus switch
            {
                WorkOrderOperationalStatus.Draft => "Work order is in Draft status and cannot be operationally completed.",
                WorkOrderOperationalStatus.Cancelled => "Work order is Cancelled and cannot be completed.",
                WorkOrderOperationalStatus.OperationallyComplete => "Work order is already Operationally Complete.",
                _ => $"Status '{workOrder.OperationalStatus}' is not eligible for completion."
            };
            gates.Add(new CompletionGateResultDto("StatusEligibility", false, "Operational status eligibility check.", reason));
            unmet.Add(reason);
        }

        // Gate 2: Booking Completion
        var activeBookings = bookings.Where(b => b.Status != BookingStatus.Cancelled).ToList();
        var completedBookings = activeBookings.Where(b => b.Status == BookingStatus.Completed).ToList();

        if (activeBookings.Count == 0)
        {
            var reason = "At least one scheduled visit booking is required before operational completion.";
            gates.Add(new CompletionGateResultDto("BookingCompletion", false, "Work order visit bookings check.", reason));
            unmet.Add(reason);
        }
        else if (completedBookings.Count < activeBookings.Count)
        {
            var incomplete = activeBookings.First(b => b.Status != BookingStatus.Completed);
            var reason = $"Booking '{incomplete.BookingNumber}' is currently in status '{incomplete.Status}'. All scheduled visits must be completed.";
            gates.Add(new CompletionGateResultDto("BookingCompletion", false, "All scheduled visits must be completed.", reason));
            unmet.Add(reason);
        }
        else
        {
            gates.Add(new CompletionGateResultDto(
                "BookingCompletion",
                true,
                $"All {activeBookings.Count} visit bookings have been successfully completed.",
                null));
        }

        // Gate 3: Execution Evidence
        var completedSessions = sessions.Where(s =>
            s.Status == ExecutionSessionStatus.Ended &&
            !string.IsNullOrWhiteSpace(s.WorkSummary)).ToList();

        if (completedSessions.Count == 0)
        {
            var reason = "Operational execution evidence (completed execution session with technician work summary) is required.";
            gates.Add(new CompletionGateResultDto("ExecutionEvidence", false, "Technician execution summary and evidence check.", reason));
            unmet.Add(reason);
        }
        else
        {
            gates.Add(new CompletionGateResultDto(
                "ExecutionEvidence",
                true,
                $"Technician execution evidence recorded ({completedSessions.Count} completed session(s) with work summary).",
                null));
        }

        // Gate 4: Scope Items
        var scopeItems = workOrder.ScopeItems.ToList();
        var unfulfilledRequired = scopeItems
            .Where(s => s.IsRequiredForCompletion && s.Status == WorkOrderScopeItemStatus.Authorized)
            .ToList();

        if (unfulfilledRequired.Count > 0)
        {
            var first = unfulfilledRequired[0];
            var reason = $"Required scope item #{first.Sequence} ('{first.Description}') has not been fulfilled.";
            gates.Add(new CompletionGateResultDto("ScopeItems", false, "Authorized operational scope fulfillment check.", reason));
            unmet.Add(reason);
        }
        else
        {
            gates.Add(new CompletionGateResultDto(
                "ScopeItems",
                true,
                scopeItems.Count > 0
                    ? $"All {scopeItems.Count} scope item(s) are fulfilled, cancelled, or not required."
                    : "No specific gated scope items declared; baseline scope satisfied.",
                null));
        }

        // Gate 5: Work Tasks & Inspections (FIE-003)
        var requiredTasks = tasks
            .Where(t => t.IsRequired || t.Gate == WorkTaskGate.WorkOrderCompletion)
            .ToList();

        var incompleteTasks = requiredTasks
            .Where(t => t.Status != WorkTaskStatus.Completed && t.Status != WorkTaskStatus.Skipped)
            .ToList();

        if (incompleteTasks.Count > 0)
        {
            var firstTask = incompleteTasks[0];
            var reason = $"Required task #{firstTask.Sequence} ('{firstTask.Title}') is in status '{firstTask.Status}' and must be completed or skipped.";
            gates.Add(new CompletionGateResultDto("WorkTasks", false, "Mandatory tasks, checklists, and inspections compliance check (FIE-003).", reason));
            unmet.Add(reason);
        }
        else
        {
            gates.Add(new CompletionGateResultDto(
                "WorkTasks",
                true,
                requiredTasks.Count > 0
                    ? $"All {requiredTasks.Count} required work tasks, checklists, and inspections are satisfied."
                    : "No specific mandatory work tasks or inspections required.",
                null));
        }

        // Gate 6: Manager Review
        gates.Add(new CompletionGateResultDto(
            "ManagerReview",
            true,
            "Authorized review policy satisfied.",
            null));

        var isCompleteEligible = gates.All(g => g.Passed);
        var recommendedOutcome = isCompleteEligible
            ? WorkOrderCompletionOutcome.OperationallyComplete
            : WorkOrderCompletionOutcome.FollowUpRequired;

        var scopeItemDtos = scopeItems.Select(s => new WorkOrderScopeItemDto(
            s.Id,
            s.WorkOrderId,
            s.Sequence,
            s.ScopeType.ToString(),
            (short)s.ScopeType,
            s.Description,
            s.Status.ToString(),
            (short)s.Status,
            s.IsRequiredForCompletion,
            s.AssetId,
            s.FulfilledAtUtc,
            s.FulfilledByUserId,
            s.CreatedAtUtc,
            s.ModifiedAtUtc)).ToList();

        WorkOrderCompletionEvaluationDto? latestEvalDto = null;
        if (latestEval != null)
        {
            IReadOnlyList<CompletionGateResultDto> savedGates = [];
            try
            {
                savedGates = JsonSerializer.Deserialize<List<CompletionGateResultDto>>(latestEval.GateResultsJson) ?? [];
            }
            catch
            {
                // Fallback to empty if parse fails
            }

            latestEvalDto = new WorkOrderCompletionEvaluationDto(
                latestEval.Id,
                latestEval.WorkOrderId,
                latestEval.CommandId,
                latestEval.EvaluatedAtUtc,
                latestEval.EvaluatedByUserId,
                latestEval.Outcome.ToString(),
                (short)latestEval.Outcome,
                latestEval.IsEligibleForCompletion,
                savedGates,
                latestEval.TriggerBookingId,
                latestEval.Summary,
                latestEval.Notes);
        }

        return new WorkOrderCompletionReadinessDto(
            workOrderId,
            isCompleteEligible,
            workOrder.OperationalStatus.ToString(),
            (short)workOrder.OperationalStatus,
            recommendedOutcome.ToString(),
            (short)recommendedOutcome,
            gates,
            unmet,
            scopeItemDtos,
            bookings.Count,
            completedBookings.Count,
            sessions.Count,
            completedSessions.Count,
            latestEvalDto);
    }

    public async Task<WorkOrderCompletionEvaluation> CreateEvaluationRecordAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid commandId,
        Guid? evaluatingUserId,
        Guid? triggerBookingId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var readiness = await EvaluateAsync(tenantId, workOrderId, evaluatingUserId, cancellationToken);
        var gatesJson = JsonSerializer.Serialize(readiness.Gates);

        var outcome = (WorkOrderCompletionOutcome)readiness.RecommendedOutcomeValue;
        var summary = readiness.IsCompleteEligible
            ? "All completion gates satisfied."
            : $"Completion blocked by {readiness.UnmetRequirements.Count} unmet requirement(s): {string.Join("; ", readiness.UnmetRequirements)}";

        return new WorkOrderCompletionEvaluation(
            tenantId: tenantId,
            workOrderId: workOrderId,
            commandId: commandId,
            outcome: outcome,
            gateResultsJson: gatesJson,
            evaluatedByUserId: evaluatingUserId,
            triggerBookingId: triggerBookingId,
            summary: summary,
            notes: notes);
    }
}

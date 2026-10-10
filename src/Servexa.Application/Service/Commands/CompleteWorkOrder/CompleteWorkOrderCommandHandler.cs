using System.Text.Json;
using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Application.Service.Services;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.CompleteWorkOrder;

public sealed class CompleteWorkOrderCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IAccountRepository accountRepository,
    ISiteRepository siteRepository,
    IAssetRepository assetRepository,
    IWorkOrderCompletionEngine completionEngine,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CompleteWorkOrderCommand, WorkOrderDto>
{
    public async Task<WorkOrderDto> Handle(CompleteWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("User context is required.");

        if (!tenantContext.HasPermission(Capabilities.WorkOrderComplete))
        {
            throw new ForbiddenAccessException($"User lacks required capability '{Capabilities.WorkOrderComplete}' to complete work orders.");
        }

        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, request.WorkOrderId, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException($"Work order with ID '{request.WorkOrderId}' was not found.");

        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete)
        {
            throw new InvalidOperationException("Work order is already operationally complete.");
        }

        // Run authoritative completion evaluation
        var evaluation = await completionEngine.CreateEvaluationRecordAsync(
            tenantId,
            request.WorkOrderId,
            request.CommandId,
            userId,
            triggerBookingId: null,
            notes: request.Notes,
            cancellationToken: cancellationToken);

        if (!evaluation.IsEligibleForCompletion)
        {
            // Persist the failed/blocked evaluation for complete auditability
            workOrder.RecordEvaluation(evaluation);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new ConflictException(evaluation.Summary ?? "Work order completion gates were not satisfied.");
        }

        // Successful evaluation -> complete the work order
        workOrder.CompleteWithEvaluation(evaluation, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to return DTO
        var serviceAccount = await accountRepository.GetByIdAsync(tenantId, workOrder.ServiceAccountId, cancellationToken);
        var billToAccount = workOrder.BillToAccountId == workOrder.ServiceAccountId
            ? serviceAccount
            : await accountRepository.GetByIdAsync(tenantId, workOrder.BillToAccountId, cancellationToken);
        var site = await siteRepository.GetByIdAsync(tenantId, workOrder.PrimarySiteId, cancellationToken);

        var assetDtos = new List<WorkOrderAssetDto>();
        foreach (var a in workOrder.Assets)
        {
            var asset = await assetRepository.GetByIdAsync(tenantId, a.AssetId, asNoTracking: true, cancellationToken);
            assetDtos.Add(new WorkOrderAssetDto(
                a.Id,
                a.AssetId,
                asset?.AssetNumber,
                null,
                a.Role.ToString(),
                (short)a.Role,
                a.SiteIdAtTime,
                a.Status.ToString(),
                (short)a.Status));
        }

        var historyDtos = workOrder.StatusHistory.Select(h => new WorkOrderStatusHistoryDto(
            h.Id,
            h.FromStatus.ToString(),
            (short)h.FromStatus,
            h.ToStatus.ToString(),
            (short)h.ToStatus,
            h.PauseReasonCode,
            h.ChangedByUserId,
            h.ChangedAtUtc,
            h.Reason)).ToList();

        var scopeItemDtos = workOrder.ScopeItems.Select(s => new WorkOrderScopeItemDto(
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

        IReadOnlyList<CompletionGateResultDto> gateResults = [];
        try
        {
            gateResults = JsonSerializer.Deserialize<List<CompletionGateResultDto>>(evaluation.GateResultsJson) ?? [];
        }
        catch
        {
        }

        var latestEvalDto = new WorkOrderCompletionEvaluationDto(
            evaluation.Id,
            evaluation.WorkOrderId,
            evaluation.CommandId,
            evaluation.EvaluatedAtUtc,
            evaluation.EvaluatedByUserId,
            evaluation.Outcome.ToString(),
            (short)evaluation.Outcome,
            evaluation.IsEligibleForCompletion,
            gateResults,
            evaluation.TriggerBookingId,
            evaluation.Summary,
            evaluation.Notes);

        return new WorkOrderDto(
            workOrder.Id,
            workOrder.TenantId,
            workOrder.WorkOrderNumber,
            workOrder.ServiceRequestId,
            workOrder.ServiceAccountId,
            serviceAccount?.DisplayName,
            workOrder.BillToAccountId,
            billToAccount?.DisplayName,
            workOrder.PrimarySiteId,
            site?.Name,
            workOrder.WorkTypeCode,
            workOrder.Priority.ToString(),
            (short)workOrder.Priority,
            workOrder.OperationalStatus.ToString(),
            (short)workOrder.OperationalStatus,
            workOrder.Summary,
            workOrder.Description,
            workOrder.PauseReasonCode,
            workOrder.PauseNote,
            workOrder.OperationallyCompletedAtUtc,
            workOrder.CreatedAtUtc,
            workOrder.ModifiedAtUtc,
            assetDtos,
            historyDtos,
            scopeItemDtos,
            latestEvalDto);
    }
}

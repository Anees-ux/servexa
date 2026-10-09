using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.TransitionWorkOrderStatus;

public sealed class TransitionWorkOrderStatusCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IAccountRepository accountRepository,
    ISiteRepository siteRepository,
    IAssetRepository assetRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<TransitionWorkOrderStatusCommand, WorkOrderDto>
{
    public async Task<WorkOrderDto> Handle(TransitionWorkOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        Guid? userId = tenantContext.UserId != Guid.Empty ? tenantContext.UserId : null;

        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, request.WorkOrderId, asNoTracking: false, cancellationToken);
        if (workOrder is null)
        {
            throw new NotFoundException($"Work order with ID '{request.WorkOrderId}' was not found.");
        }

        switch (request.TargetStatus)
        {
            case WorkOrderOperationalStatus.Approved:
                if (workOrder.OperationalStatus == WorkOrderOperationalStatus.Draft)
                {
                    workOrder.Approve(userId);
                }
                else if (workOrder.OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete)
                {
                    if (string.IsNullOrWhiteSpace(request.Reason))
                    {
                        throw new ValidationException("A non-empty reason is mandatory when reopening an operationally complete work order.");
                    }
                    workOrder.Reopen(request.Reason, userId);
                }
                else
                {
                    throw new ValidationException($"Cannot transition work order from {workOrder.OperationalStatus} to Approved.");
                }
                break;

            case WorkOrderOperationalStatus.Scheduled:
                workOrder.MarkScheduled(userId);
                break;

            case WorkOrderOperationalStatus.InProgress:
                workOrder.StartProgress(userId);
                break;

            case WorkOrderOperationalStatus.Paused:
                workOrder.Pause(request.PauseReasonCode ?? "GENERAL", request.PauseNote, userId);
                break;

            case WorkOrderOperationalStatus.OperationallyComplete:
                workOrder.Complete(userId);
                break;

            case WorkOrderOperationalStatus.Cancelled:
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    throw new ValidationException("A non-empty reason is mandatory when cancelling a work order.");
                }
                workOrder.Cancel(request.Reason, userId);
                break;

            default:
                throw new ValidationException($"Unsupported target operational status '{request.TargetStatus}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch display names for DTO
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
            h.Reason)).OrderByDescending(h => h.ChangedAtUtc).ToList();

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
            historyDtos);
    }
}

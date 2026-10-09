using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;

namespace Servexa.Application.Service.Queries.GetWorkOrderById;

public sealed record GetWorkOrderByIdQuery(Guid Id) : IRequest<WorkOrderDto>;

public sealed class GetWorkOrderByIdQueryHandler(
    IWorkOrderRepository workOrderRepository,
    IAccountRepository accountRepository,
    ISiteRepository siteRepository,
    IAssetRepository assetRepository,
    ITenantContext tenantContext) : IRequestHandler<GetWorkOrderByIdQuery, WorkOrderDto>
{
    public async Task<WorkOrderDto> Handle(GetWorkOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, request.Id, asNoTracking: true, cancellationToken);
        if (workOrder is null)
        {
            throw new NotFoundException($"Work order with ID '{request.Id}' was not found.");
        }

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

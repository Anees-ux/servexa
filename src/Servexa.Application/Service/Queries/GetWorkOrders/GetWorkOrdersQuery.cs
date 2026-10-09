using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;

namespace Servexa.Application.Service.Queries.GetWorkOrders;

public sealed record GetWorkOrdersQuery(
    Guid? ServiceAccountId = null,
    Guid? PrimarySiteId = null,
    short? OperationalStatus = null,
    short? Priority = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<WorkOrderDto>>;

public sealed class GetWorkOrdersQueryHandler(
    IWorkOrderRepository workOrderRepository,
    IAccountRepository accountRepository,
    ISiteRepository siteRepository,
    ITenantContext tenantContext) : IRequestHandler<GetWorkOrdersQuery, PagedResult<WorkOrderDto>>
{
    public async Task<PagedResult<WorkOrderDto>> Handle(GetWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var items = await workOrderRepository.GetWorkOrdersAsync(
            tenantId,
            request.ServiceAccountId,
            request.PrimarySiteId,
            request.OperationalStatus,
            request.Priority,
            request.Search,
            skip,
            pageSize,
            cancellationToken);

        var totalCount = await workOrderRepository.GetCountAsync(
            tenantId,
            request.ServiceAccountId,
            request.PrimarySiteId,
            request.OperationalStatus,
            request.Priority,
            request.Search,
            cancellationToken);

        // Pre-fetch accounts and sites for mapping
        var accountIds = items.SelectMany(w => new[] { w.ServiceAccountId, w.BillToAccountId }).Distinct().ToList();
        var accounts = new Dictionary<Guid, string>();
        foreach (var aid in accountIds)
        {
            var acc = await accountRepository.GetByIdAsync(tenantId, aid, cancellationToken);
            if (acc is not null)
            {
                accounts[aid] = acc.DisplayName;
            }
        }

        var siteIds = items.Select(w => w.PrimarySiteId).Distinct().ToList();
        var sites = new Dictionary<Guid, string>();
        foreach (var sid in siteIds)
        {
            var s = await siteRepository.GetByIdAsync(tenantId, sid, cancellationToken);
            if (s is not null)
            {
                sites[sid] = s.Name;
            }
        }

        var dtos = items.Select(w =>
        {
            accounts.TryGetValue(w.ServiceAccountId, out var serviceAccountName);
            accounts.TryGetValue(w.BillToAccountId, out var billToAccountName);
            sites.TryGetValue(w.PrimarySiteId, out var siteName);

            var assetDtos = w.Assets.Select(a => new WorkOrderAssetDto(
                a.Id,
                a.AssetId,
                null,
                null,
                a.Role.ToString(),
                (short)a.Role,
                a.SiteIdAtTime,
                a.Status.ToString(),
                (short)a.Status)).ToList();

            var historyDtos = w.StatusHistory.Select(h => new WorkOrderStatusHistoryDto(
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
                w.Id,
                w.TenantId,
                w.WorkOrderNumber,
                w.ServiceRequestId,
                w.ServiceAccountId,
                serviceAccountName,
                w.BillToAccountId,
                billToAccountName,
                w.PrimarySiteId,
                siteName,
                w.WorkTypeCode,
                w.Priority.ToString(),
                (short)w.Priority,
                w.OperationalStatus.ToString(),
                (short)w.OperationalStatus,
                w.Summary,
                w.Description,
                w.PauseReasonCode,
                w.PauseNote,
                w.OperationallyCompletedAtUtc,
                w.CreatedAtUtc,
                w.ModifiedAtUtc,
                assetDtos,
                historyDtos);
        }).ToList();

        return new PagedResult<WorkOrderDto>(dtos, totalCount, pageNumber, pageSize);
    }
}

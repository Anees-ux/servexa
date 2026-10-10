using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Mappers;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;

namespace Servexa.Application.Scheduling.Queries.GetBookings;

public sealed class GetBookingsQueryHandler(
    IBookingRepository bookingRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IResourceRepository resourceRepository,
    ITenantContext tenantContext) : IRequestHandler<GetBookingsQuery, PagedResult<BookingDto>>
{
    public async Task<PagedResult<BookingDto>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var skip = (request.PageNumber - 1) * request.PageSize;

        var items = await bookingRepository.GetBookingsAsync(
            tenantId,
            request.WorkOrderId,
            request.SiteId,
            request.Status,
            request.DispatchStatus,
            request.FromUtc,
            request.ToUtc,
            skip,
            request.PageSize,
            cancellationToken);

        var totalCount = await bookingRepository.GetCountAsync(
            tenantId,
            request.WorkOrderId,
            request.SiteId,
            request.Status,
            request.DispatchStatus,
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        // Batch lookups to prevent N+1 queries
        var workOrderIds = items.Select(b => b.WorkOrderId).Distinct().ToList();
        var siteIds = items.Select(b => b.SiteId).Distinct().ToList();
        var resourceIds = items.SelectMany(b => b.Assignments).Select(a => a.ResourceId).Distinct().ToList();

        var workOrders = new Dictionary<Guid, (string Number, string Summary, Guid ServiceAccountId)>();
        foreach (var woId in workOrderIds)
        {
            var wo = await workOrderRepository.GetByIdAsync(tenantId, woId, asNoTracking: true, cancellationToken);
            if (wo is not null)
            {
                workOrders[woId] = (wo.WorkOrderNumber, wo.Summary, wo.ServiceAccountId);
            }
        }

        var sites = new Dictionary<Guid, string>();
        foreach (var sid in siteIds)
        {
            var s = await siteRepository.GetByIdAsync(tenantId, sid, cancellationToken);
            if (s is not null)
            {
                sites[sid] = s.Name;
            }
        }

        var accountIds = workOrders.Values.Select(v => v.ServiceAccountId).Distinct().ToList();
        var accounts = new Dictionary<Guid, string>();
        foreach (var aid in accountIds)
        {
            var a = await accountRepository.GetByIdAsync(tenantId, aid, cancellationToken);
            if (a is not null)
            {
                accounts[aid] = a.DisplayName ?? a.LegalName;
            }
        }

        var resources = new Dictionary<Guid, (string Code, string Name)>();
        foreach (var rid in resourceIds)
        {
            var r = await resourceRepository.GetByIdAsync(tenantId, rid, asNoTracking: true, cancellationToken);
            if (r is not null)
            {
                resources[rid] = (r.ResourceCode, r.DisplayName);
            }
        }

        var dtos = items.Select(b =>
        {
            workOrders.TryGetValue(b.WorkOrderId, out var wo);
            sites.TryGetValue(b.SiteId, out var siteName);
            string? accName = null;
            if (wo != default && accounts.TryGetValue(wo.ServiceAccountId, out var name))
            {
                accName = name;
            }

            return BookingDtoMapper.ToDto(
                b,
                wo.Number,
                wo.Summary,
                siteName,
                wo != default ? wo.ServiceAccountId : null,
                accName,
                resources);
        }).ToList();

        return new PagedResult<BookingDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

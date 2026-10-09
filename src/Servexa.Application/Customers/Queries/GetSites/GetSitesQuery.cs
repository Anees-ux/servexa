using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;

namespace Servexa.Application.Customers.Queries.GetSites;

public sealed record GetSitesQuery(
    Guid? BranchId = null,
    Guid? AccountId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<SiteDto>>;

public sealed class GetSitesQueryHandler(
    ISiteRepository siteRepository,
    ITenantContext tenantContext) : IRequestHandler<GetSitesQuery, PagedResult<SiteDto>>
{
    public async Task<PagedResult<SiteDto>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var items = await siteRepository.GetSitesAsync(
            tenantId,
            request.BranchId,
            request.AccountId,
            request.Search,
            skip,
            pageSize,
            cancellationToken);

        var totalCount = await siteRepository.GetCountAsync(
            tenantId,
            request.BranchId,
            request.AccountId,
            request.Search,
            cancellationToken);

        var dtos = new List<SiteDto>(items.Count);
        foreach (var s in items)
        {
            var rels = await siteRepository.GetRelationshipsBySiteIdAsync(tenantId, s.Id, cancellationToken);
            var primaryAccountId = rels.FirstOrDefault(r => r.IsDefault)?.AccountId;

            dtos.Add(new SiteDto(
                s.Id,
                s.TenantId,
                s.SiteNumber,
                s.Name,
                s.BranchId,
                s.TerritoryId,
                s.TimeZoneId,
                s.AddressLine1,
                s.AddressLine2,
                s.City,
                s.StateProvince,
                s.PostalCode,
                s.CountryCode,
                s.Latitude,
                s.Longitude,
                s.Status.ToString(),
                s.AccessNotes,
                s.HazardNotes,
                s.CreatedAtUtc,
                s.ModifiedAtUtc,
                primaryAccountId));
        }

        return new PagedResult<SiteDto>(dtos, totalCount, pageNumber, pageSize);
    }
}

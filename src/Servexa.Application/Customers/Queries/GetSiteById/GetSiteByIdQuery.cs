using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;

namespace Servexa.Application.Customers.Queries.GetSiteById;

public sealed record GetSiteByIdQuery(Guid SiteId) : IRequest<SiteDto>;

public sealed class GetSiteByIdQueryHandler(
    ISiteRepository siteRepository,
    ITenantContext tenantContext) : IRequestHandler<GetSiteByIdQuery, SiteDto>
{
    public async Task<SiteDto> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var site = await siteRepository.GetByIdAsync(tenantId, request.SiteId, cancellationToken);

        if (site == null)
        {
            throw new NotFoundException($"Site with ID '{request.SiteId}' was not found.");
        }

        var rels = await siteRepository.GetRelationshipsBySiteIdAsync(tenantId, site.Id, cancellationToken);
        var primaryAccountId = rels.FirstOrDefault(r => r.IsDefault)?.AccountId;

        return new SiteDto(
            site.Id,
            site.TenantId,
            site.SiteNumber,
            site.Name,
            site.BranchId,
            site.TerritoryId,
            site.TimeZoneId,
            site.AddressLine1,
            site.AddressLine2,
            site.City,
            site.StateProvince,
            site.PostalCode,
            site.CountryCode,
            site.Latitude,
            site.Longitude,
            site.Status.ToString(),
            site.AccessNotes,
            site.HazardNotes,
            site.CreatedAtUtc,
            site.ModifiedAtUtc,
            primaryAccountId);
    }
}

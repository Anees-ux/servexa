using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Customers.Enums;

namespace Servexa.Application.Customers.Commands.CreateSite;

public sealed class CreateSiteCommandHandler(
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateSiteCommand, SiteDto>
{
    public async Task<SiteDto> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var exists = await siteRepository.ExistsAsync(tenantId, request.SiteNumber, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"A site with number '{request.SiteNumber}' already exists for this tenant.");
        }

        if (request.PrimaryAccountId.HasValue)
        {
            var account = await accountRepository.GetByIdAsync(tenantId, request.PrimaryAccountId.Value, cancellationToken);
            if (account == null)
            {
                throw new NotFoundException($"Primary account with ID '{request.PrimaryAccountId.Value}' was not found.");
            }
        }

        var site = new Site(
            tenantId: tenantId,
            siteNumber: request.SiteNumber,
            name: request.Name,
            branchId: request.BranchId,
            timeZoneId: request.TimeZoneId,
            addressLine1: request.AddressLine1,
            city: request.City,
            stateProvince: request.StateProvince,
            postalCode: request.PostalCode,
            countryCode: request.CountryCode,
            addressLine2: request.AddressLine2,
            territoryId: request.TerritoryId,
            latitude: request.Latitude,
            longitude: request.Longitude,
            accessNotes: request.AccessNotes,
            hazardNotes: request.HazardNotes);

        await siteRepository.AddAsync(site, cancellationToken);

        if (request.PrimaryAccountId.HasValue)
        {
            var relationship = new SiteAccountRelationship(
                tenantId: tenantId,
                siteId: site.Id,
                accountId: request.PrimaryAccountId.Value,
                relationType: SiteAccountRelationType.ServiceCustomer,
                isDefault: true);

            await siteRepository.AddRelationshipAsync(relationship, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
            request.PrimaryAccountId);
    }
}

using MediatR;
using Servexa.Application.Customers.Dtos;

namespace Servexa.Application.Customers.Commands.CreateSite;

public sealed record CreateSiteCommand(
    string SiteNumber,
    string Name,
    Guid BranchId,
    string TimeZoneId,
    string AddressLine1,
    string City,
    string StateProvince,
    string PostalCode,
    string CountryCode = "US",
    string? AddressLine2 = null,
    Guid? TerritoryId = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    string? AccessNotes = null,
    string? HazardNotes = null,
    Guid? PrimaryAccountId = null) : IRequest<SiteDto>;

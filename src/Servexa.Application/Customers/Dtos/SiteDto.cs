namespace Servexa.Application.Customers.Dtos;

public sealed record SiteDto(
    Guid Id,
    Guid TenantId,
    string SiteNumber,
    string Name,
    Guid BranchId,
    Guid? TerritoryId,
    string TimeZoneId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateProvince,
    string PostalCode,
    string CountryCode,
    decimal? Latitude,
    decimal? Longitude,
    string Status,
    string? AccessNotes,
    string? HazardNotes,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc,
    Guid? PrimaryAccountId);

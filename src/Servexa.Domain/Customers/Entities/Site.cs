using Servexa.Domain.Customers.Enums;

namespace Servexa.Domain.Customers.Entities;

/// <summary>
/// Site represents an operational physical service location.
/// Operational identity survives address change, ownership change and billing change.
/// </summary>
public class Site
{
    // Parameterless constructor for EF Core instantiation
    private Site()
    {
    }

    public Site(
        Guid tenantId,
        string siteNumber,
        string name,
        Guid branchId,
        string timeZoneId,
        string addressLine1,
        string city,
        string stateProvince,
        string postalCode,
        string countryCode = "US",
        string? addressLine2 = null,
        Guid? territoryId = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? accessNotes = null,
        string? hazardNotes = null,
        SiteStatus status = SiteStatus.Active,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId cannot be empty.", nameof(branchId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(siteNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateProvince);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
        {
            throw new ArgumentException("Country code must be a 2-character ISO code.", nameof(countryCode));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        SiteNumber = siteNumber.Trim().ToUpperInvariant();
        Name = name.Trim();
        BranchId = branchId;
        TerritoryId = territoryId;
        TimeZoneId = timeZoneId.Trim();
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        StateProvince = stateProvince.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        AccessNotes = string.IsNullOrWhiteSpace(accessNotes) ? null : accessNotes.Trim();
        HazardNotes = string.IsNullOrWhiteSpace(hazardNotes) ? null : hazardNotes.Trim();
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string SiteNumber { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid BranchId { get; private set; }
    public Guid? TerritoryId { get; private set; }
    public string TimeZoneId { get; private set; } = null!;
    public string AddressLine1 { get; private set; } = null!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = null!;
    public string StateProvince { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? LongLongitude => Longitude; // Alias if needed
    public decimal? Longitude { get; private set; }
    public SiteStatus Status { get; private set; }
    public string? AccessNotes { get; private set; }
    public string? HazardNotes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateDetails(
        string name,
        Guid branchId,
        Guid? territoryId,
        string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("BranchId cannot be empty.", nameof(branchId));
        }

        Name = name.Trim();
        BranchId = branchId;
        TerritoryId = territoryId;
        TimeZoneId = timeZoneId.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateAddress(
        string addressLine1,
        string? addressLine2,
        string city,
        string stateProvince,
        string postalCode,
        string countryCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateProvince);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
        {
            throw new ArgumentException("Country code must be a 2-character ISO code.", nameof(countryCode));
        }

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        StateProvince = stateProvince.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateAccessAndHazards(string? accessNotes, string? hazardNotes)
    {
        AccessNotes = string.IsNullOrWhiteSpace(accessNotes) ? null : accessNotes.Trim();
        HazardNotes = string.IsNullOrWhiteSpace(hazardNotes) ? null : hazardNotes.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(SiteStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

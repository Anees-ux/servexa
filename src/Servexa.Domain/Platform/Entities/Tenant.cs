using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// Tenant represents the foundational multi-tenant boundary and lifecycle owner.
/// </summary>
public class Tenant
{
    // Parameterless constructor for EF Core instantiation
    private Tenant()
    {
    }

    public Tenant(
        string tenantCode,
        string legalName,
        string displayName,
        string defaultTimeZoneId,
        string defaultCurrencyCode,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultTimeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCurrencyCode);

        Id = id ?? Guid.CreateVersion7();
        TenantCode = tenantCode.Trim();
        LegalName = legalName.Trim();
        DisplayName = displayName.Trim();
        Status = TenantStatus.Active;
        DefaultTimeZoneId = defaultTimeZoneId.Trim();
        DefaultCurrencyCode = defaultCurrencyCode.Trim().ToUpperInvariant();
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string TenantCode { get; private set; } = null!;
    public string LegalName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public TenantStatus Status { get; private set; }
    public string DefaultTimeZoneId { get; private set; } = null!;
    public string DefaultCurrencyCode { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateProfile(string legalName, string displayName, string defaultTimeZoneId, string defaultCurrencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultTimeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCurrencyCode);

        LegalName = legalName.Trim();
        DisplayName = displayName.Trim();
        DefaultTimeZoneId = defaultTimeZoneId.Trim();
        DefaultCurrencyCode = defaultCurrencyCode.Trim().ToUpperInvariant();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(TenantStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

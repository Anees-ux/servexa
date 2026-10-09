using Servexa.Domain.Customers.Enums;

namespace Servexa.Domain.Customers.Entities;

/// <summary>
/// SiteAccountRelationship links a physical Site to one or more Accounts (e.g. ServiceCustomer, BillTo, Owner).
/// </summary>
public class SiteAccountRelationship
{
    // Parameterless constructor for EF Core instantiation
    private SiteAccountRelationship()
    {
    }

    public SiteAccountRelationship(
        Guid tenantId,
        Guid siteId,
        Guid accountId,
        SiteAccountRelationType relationType = SiteAccountRelationType.ServiceCustomer,
        bool isDefault = true,
        DateTime? effectiveFromUtc = null,
        DateTime? effectiveToUtc = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("SiteId cannot be empty.", nameof(siteId));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("AccountId cannot be empty.", nameof(accountId));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        SiteId = siteId;
        AccountId = accountId;
        RelationType = relationType;
        IsDefault = isDefault;
        EffectiveFromUtc = effectiveFromUtc ?? DateTime.UtcNow;
        EffectiveToUtc = effectiveToUtc;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid AccountId { get; private set; }
    public SiteAccountRelationType RelationType { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }

    public void EndRelationship(DateTime effectiveToUtc)
    {
        EffectiveToUtc = effectiveToUtc;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

using Servexa.Domain.Customers.Enums;

namespace Servexa.Domain.Customers.Entities;

/// <summary>
/// Account represents the legal, billing customer identity and commercial attributes.
/// Distinct from Contact (individual person) and Site (service location).
/// </summary>
public class Account
{
    // Parameterless constructor for EF Core instantiation
    private Account()
    {
    }

    public Account(
        Guid tenantId,
        string accountNumber,
        string legalName,
        string displayName,
        AccountType accountType = AccountType.Commercial,
        AccountStatus status = AccountStatus.Active,
        Guid? defaultBranchId = null,
        int? paymentTermsDays = 30,
        string currencyCode = "USD",
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            throw new ArgumentException("Currency code must be a 3-character ISO code.", nameof(currencyCode));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        AccountNumber = accountNumber.Trim().ToUpperInvariant();
        LegalName = legalName.Trim();
        DisplayName = displayName.Trim();
        AccountType = accountType;
        Status = status;
        DefaultBranchId = defaultBranchId;
        PaymentTermsDays = paymentTermsDays;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        IsCreditHold = false;
        CreditHoldReason = null;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string AccountNumber { get; private set; } = null!;
    public string LegalName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public AccountType AccountType { get; private set; }
    public AccountStatus Status { get; private set; }
    public Guid? DefaultBranchId { get; private set; }
    public int? PaymentTermsDays { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public bool IsCreditHold { get; private set; }
    public string? CreditHoldReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateDetails(
        string legalName,
        string displayName,
        AccountType accountType,
        Guid? defaultBranchId,
        int? paymentTermsDays,
        string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            throw new ArgumentException("Currency code must be a 3-character ISO code.", nameof(currencyCode));
        }

        LegalName = legalName.Trim();
        DisplayName = displayName.Trim();
        AccountType = accountType;
        DefaultBranchId = defaultBranchId;
        PaymentTermsDays = paymentTermsDays;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void PutOnCreditHold(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsCreditHold = true;
        CreditHoldReason = reason.Trim();
        Status = AccountStatus.OnHold;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void ReleaseCreditHold()
    {
        IsCreditHold = false;
        CreditHoldReason = null;
        if (Status == AccountStatus.OnHold)
        {
            Status = AccountStatus.Active;
        }
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(AccountStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

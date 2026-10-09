namespace Servexa.Application.Customers.Dtos;

public sealed record AccountDto(
    Guid Id,
    Guid TenantId,
    string AccountNumber,
    string LegalName,
    string DisplayName,
    string AccountType,
    string Status,
    Guid? DefaultBranchId,
    int? PaymentTermsDays,
    string CurrencyCode,
    bool IsCreditHold,
    string? CreditHoldReason,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc);

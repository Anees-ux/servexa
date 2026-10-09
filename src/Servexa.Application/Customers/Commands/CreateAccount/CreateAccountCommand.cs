using MediatR;
using Servexa.Application.Customers.Dtos;
using Servexa.Domain.Customers.Enums;

namespace Servexa.Application.Customers.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string AccountNumber,
    string LegalName,
    string DisplayName,
    AccountType AccountType = AccountType.Commercial,
    Guid? DefaultBranchId = null,
    int? PaymentTermsDays = 30,
    string CurrencyCode = "USD") : IRequest<AccountDto>;

using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Domain.Customers.Entities;

namespace Servexa.Application.Customers.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var exists = await accountRepository.ExistsAsync(tenantId, request.AccountNumber, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"An account with number '{request.AccountNumber}' already exists for this tenant.");
        }

        var account = new Account(
            tenantId: tenantId,
            accountNumber: request.AccountNumber,
            legalName: request.LegalName,
            displayName: request.DisplayName,
            accountType: request.AccountType,
            defaultBranchId: request.DefaultBranchId,
            paymentTermsDays: request.PaymentTermsDays,
            currencyCode: request.CurrencyCode);

        await accountRepository.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccountDto(
            account.Id,
            account.TenantId,
            account.AccountNumber,
            account.LegalName,
            account.DisplayName,
            account.AccountType.ToString(),
            account.Status.ToString(),
            account.DefaultBranchId,
            account.PaymentTermsDays,
            account.CurrencyCode,
            account.IsCreditHold,
            account.CreditHoldReason,
            account.CreatedAtUtc,
            account.ModifiedAtUtc);
    }
}

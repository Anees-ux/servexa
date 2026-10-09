using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;

namespace Servexa.Application.Customers.Queries.GetAccountById;

public sealed record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto>;

public sealed class GetAccountByIdQueryHandler(
    IAccountRepository accountRepository,
    ITenantContext tenantContext) : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var account = await accountRepository.GetByIdAsync(tenantId, request.AccountId, cancellationToken);

        if (account == null)
        {
            throw new NotFoundException($"Account with ID '{request.AccountId}' was not found.");
        }

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

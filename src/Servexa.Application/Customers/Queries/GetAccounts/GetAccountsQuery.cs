using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Repositories;

namespace Servexa.Application.Customers.Queries.GetAccounts;

public sealed record GetAccountsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AccountDto>>;

public sealed class GetAccountsQueryHandler(
    IAccountRepository accountRepository,
    ITenantContext tenantContext) : IRequestHandler<GetAccountsQuery, PagedResult<AccountDto>>
{
    public async Task<PagedResult<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var items = await accountRepository.GetAccountsAsync(tenantId, request.Search, skip, pageSize, cancellationToken);
        var totalCount = await accountRepository.GetCountAsync(tenantId, request.Search, cancellationToken);

        var dtos = items.Select(a => new AccountDto(
            a.Id,
            a.TenantId,
            a.AccountNumber,
            a.LegalName,
            a.DisplayName,
            a.AccountType.ToString(),
            a.Status.ToString(),
            a.DefaultBranchId,
            a.PaymentTermsDays,
            a.CurrencyCode,
            a.IsCreditHold,
            a.CreditHoldReason,
            a.CreatedAtUtc,
            a.ModifiedAtUtc)).ToList();

        return new PagedResult<AccountDto>(dtos, totalCount, pageNumber, pageSize);
    }
}

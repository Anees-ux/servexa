using Microsoft.EntityFrameworkCore;
using Servexa.Application.Customers.Repositories;
using Servexa.Domain.Customers.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(ServexaDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid tenantId, Guid accountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == accountId, cancellationToken);
    }

    public async Task<Account?> GetByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
    {
        var normalized = accountNumber.Trim().ToUpperInvariant();
        return await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountNumber == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAccountsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(a => a.AccountNumber.Contains(trimmed)
                                  || a.DisplayName.Contains(trimmed)
                                  || a.LegalName.Contains(trimmed));
        }

        return await query
            .OrderBy(a => a.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(a => a.AccountNumber.Contains(trimmed)
                                  || a.DisplayName.Contains(trimmed)
                                  || a.LegalName.Contains(trimmed));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
    {
        var normalized = accountNumber.Trim().ToUpperInvariant();
        return await dbContext.Accounts
            .AnyAsync(a => a.TenantId == tenantId && a.AccountNumber == normalized, cancellationToken);
    }
}

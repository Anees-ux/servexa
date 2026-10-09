using Servexa.Domain.Customers.Entities;

namespace Servexa.Application.Customers.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid tenantId, Guid accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetAccountsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default);
}

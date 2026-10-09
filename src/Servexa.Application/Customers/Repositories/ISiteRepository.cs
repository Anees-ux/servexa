using Servexa.Domain.Customers.Entities;

namespace Servexa.Application.Customers.Repositories;

public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default);
    Task<Site?> GetBySiteNumberAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Site>> GetSitesAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, CancellationToken cancellationToken = default);
    Task AddAsync(Site site, CancellationToken cancellationToken = default);
    Task AddRelationshipAsync(SiteAccountRelationship relationship, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteAccountRelationship>> GetRelationshipsBySiteIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default);
}

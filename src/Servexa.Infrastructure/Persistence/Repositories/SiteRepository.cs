using Microsoft.EntityFrameworkCore;
using Servexa.Application.Customers.Repositories;
using Servexa.Domain.Customers.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class SiteRepository(ServexaDbContext dbContext) : ISiteRepository
{
    public async Task<Site?> GetByIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Sites
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == siteId, cancellationToken);
    }

    public async Task<Site?> GetBySiteNumberAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default)
    {
        var normalized = siteNumber.Trim().ToUpperInvariant();
        return await dbContext.Sites
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SiteNumber == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Site>> GetSitesAsync(
        Guid tenantId,
        Guid? branchId,
        Guid? accountId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sites
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        if (accountId.HasValue)
        {
            var siteIds = dbContext.SiteAccountRelationships
                .Where(r => r.TenantId == tenantId && r.AccountId == accountId.Value)
                .Select(r => r.SiteId);

            query = query.Where(s => siteIds.Contains(s.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(s => s.SiteNumber.Contains(trimmed)
                                  || s.Name.Contains(trimmed)
                                  || s.City.Contains(trimmed));
        }

        return await query
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid tenantId,
        Guid? branchId,
        Guid? accountId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Sites
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (branchId.HasValue)
        {
            query = query.Where(s => s.BranchId == branchId.Value);
        }

        if (accountId.HasValue)
        {
            var siteIds = dbContext.SiteAccountRelationships
                .Where(r => r.TenantId == tenantId && r.AccountId == accountId.Value)
                .Select(r => r.SiteId);

            query = query.Where(s => siteIds.Contains(s.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(s => s.SiteNumber.Contains(trimmed)
                                  || s.Name.Contains(trimmed)
                                  || s.City.Contains(trimmed));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        await dbContext.Sites.AddAsync(site, cancellationToken);
    }

    public async Task AddRelationshipAsync(SiteAccountRelationship relationship, CancellationToken cancellationToken = default)
    {
        await dbContext.SiteAccountRelationships.AddAsync(relationship, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default)
    {
        var normalized = siteNumber.Trim().ToUpperInvariant();
        return await dbContext.Sites
            .AnyAsync(s => s.TenantId == tenantId && s.SiteNumber == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<SiteAccountRelationship>> GetRelationshipsBySiteIdAsync(
        Guid tenantId,
        Guid siteId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.SiteAccountRelationships
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.SiteId == siteId)
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class ResourceRepository(ServexaDbContext dbContext) : IResourceRepository
{
    public async Task<Resource?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Resources.Where(r => r.TenantId == tenantId && r.Id == id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Resource?> GetByResourceCodeAsync(Guid tenantId, string resourceCode, CancellationToken cancellationToken = default)
    {
        var norm = resourceCode.Trim().ToUpperInvariant();
        return await dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.ResourceCode == norm, cancellationToken);
    }

    public async Task<Resource?> GetByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> GetResourcesAsync(
        Guid tenantId,
        short? status,
        short? resourceType,
        Guid? branchId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Resources
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (status.HasValue)
        {
            var st = (ResourceStatus)status.Value;
            query = query.Where(r => r.Status == st);
        }

        if (resourceType.HasValue)
        {
            var rt = (ResourceType)resourceType.Value;
            query = query.Where(r => r.ResourceType == rt);
        }

        if (branchId.HasValue)
        {
            query = query.Where(r => r.HomeBranchId == branchId.Value);
        }

        return await query
            .OrderBy(r => r.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid tenantId,
        short? status,
        short? resourceType,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Resources
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (status.HasValue)
        {
            var st = (ResourceStatus)status.Value;
            query = query.Where(r => r.Status == st);
        }

        if (resourceType.HasValue)
        {
            var rt = (ResourceType)resourceType.Value;
            query = query.Where(r => r.ResourceType == rt);
        }

        if (branchId.HasValue)
        {
            query = query.Where(r => r.HomeBranchId == branchId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        await dbContext.Resources.AddAsync(resource, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string resourceCode, CancellationToken cancellationToken = default)
    {
        var norm = resourceCode.Trim().ToUpperInvariant();
        return await dbContext.Resources
            .AnyAsync(r => r.TenantId == tenantId && r.ResourceCode == norm, cancellationToken);
    }
}

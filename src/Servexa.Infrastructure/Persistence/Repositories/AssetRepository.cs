using Microsoft.EntityFrameworkCore;
using Servexa.Application.Assets.Repositories;
using Servexa.Domain.Assets.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class AssetRepository(ServexaDbContext dbContext) : IAssetRepository
{
    public async Task<Asset?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Assets
            .Include(a => a.LifecycleEvents)
            .Where(a => a.TenantId == tenantId && a.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Asset?> GetByAssetNumberAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default)
    {
        var norm = assetNumber.Trim().ToUpperInvariant();
        return await dbContext.Assets
            .AsNoTracking()
            .Include(a => a.LifecycleEvents)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AssetNumber == norm, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetAssetsAsync(
        Guid tenantId,
        Guid? siteId,
        Guid? ownerAccountId,
        Guid? equipmentModelId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Assets
            .AsNoTracking()
            .Include(a => a.LifecycleEvents)
            .Where(a => a.TenantId == tenantId);

        if (siteId.HasValue)
        {
            query = query.Where(a => a.CurrentSiteId == siteId.Value);
        }

        if (ownerAccountId.HasValue)
        {
            query = query.Where(a => a.CurrentOwnerAccountId == ownerAccountId.Value);
        }

        if (equipmentModelId.HasValue)
        {
            query = query.Where(a => a.EquipmentModelId == equipmentModelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(a => a.AssetNumber.Contains(trimmed)
                                  || (a.SerialNumber != null && a.SerialNumber.Contains(trimmed)));
        }

        return await query
            .OrderBy(a => a.AssetNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid tenantId,
        Guid? siteId,
        Guid? ownerAccountId,
        Guid? equipmentModelId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Assets
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (siteId.HasValue)
        {
            query = query.Where(a => a.CurrentSiteId == siteId.Value);
        }

        if (ownerAccountId.HasValue)
        {
            query = query.Where(a => a.CurrentOwnerAccountId == ownerAccountId.Value);
        }

        if (equipmentModelId.HasValue)
        {
            query = query.Where(a => a.EquipmentModelId == equipmentModelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(a => a.AssetNumber.Contains(trimmed)
                                  || (a.SerialNumber != null && a.SerialNumber.Contains(trimmed)));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await dbContext.Assets.AddAsync(asset, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default)
    {
        var norm = assetNumber.Trim().ToUpperInvariant();
        return await dbContext.Assets
            .AnyAsync(a => a.TenantId == tenantId && a.AssetNumber == norm, cancellationToken);
    }
}

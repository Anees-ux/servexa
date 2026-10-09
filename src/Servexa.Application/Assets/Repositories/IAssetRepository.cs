using Servexa.Domain.Assets.Entities;

namespace Servexa.Application.Assets.Repositories;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<Asset?> GetByAssetNumberAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> GetAssetsAsync(
        Guid tenantId,
        Guid? siteId,
        Guid? ownerAccountId,
        Guid? equipmentModelId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(
        Guid tenantId,
        Guid? siteId,
        Guid? ownerAccountId,
        Guid? equipmentModelId,
        string? search,
        CancellationToken cancellationToken = default);
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default);
}

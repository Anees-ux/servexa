using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Application.Scheduling.Repositories;

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<Resource?> GetByResourceCodeAsync(Guid tenantId, string resourceCode, CancellationToken cancellationToken = default);
    Task<Resource?> GetByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> GetResourcesAsync(
        Guid tenantId,
        short? status,
        short? resourceType,
        Guid? branchId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(
        Guid tenantId,
        short? status,
        short? resourceType,
        Guid? branchId,
        CancellationToken cancellationToken = default);
    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string resourceCode, CancellationToken cancellationToken = default);
}

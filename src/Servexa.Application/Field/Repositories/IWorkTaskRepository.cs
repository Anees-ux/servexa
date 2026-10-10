using Servexa.Domain.Field.Entities;

namespace Servexa.Application.Field.Repositories;

public interface IWorkTaskRepository
{
    Task<WorkTask?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkTask>> GetByWorkOrderIdAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid? assignmentId = null,
        Guid? assetId = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);
    Task AddAsync(WorkTask task, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkTask task, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using Servexa.Application.Field.Repositories;
using Servexa.Domain.Field.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class WorkTaskRepository(ServexaDbContext dbContext) : IWorkTaskRepository
{
    public async Task<WorkTask?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkTasks
            .Where(t => t.TenantId == tenantId && t.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkTask>> GetByWorkOrderIdAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid? assignmentId = null,
        Guid? assetId = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkTasks
            .Where(t => t.TenantId == tenantId && t.WorkOrderId == workOrderId);

        if (assignmentId.HasValue)
        {
            query = query.Where(t => t.AssignmentId == assignmentId.Value);
        }

        if (assetId.HasValue)
        {
            query = query.Where(t => t.AssetId == assetId.Value);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .OrderBy(t => t.Sequence)
            .ThenBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkTasks.AddAsync(task, cancellationToken);
    }

    public Task UpdateAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        dbContext.WorkTasks.Update(task);
        return Task.CompletedTask;
    }
}

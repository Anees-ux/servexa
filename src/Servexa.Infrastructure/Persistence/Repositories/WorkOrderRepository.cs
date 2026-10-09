using Microsoft.EntityFrameworkCore;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Service.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class WorkOrderRepository(ServexaDbContext dbContext) : IWorkOrderRepository
{
    public async Task<WorkOrder?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkOrders
            .Include(w => w.Assets)
            .Include(w => w.StatusHistory)
            .Where(w => w.TenantId == tenantId && w.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkOrder?> GetByNumberAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default)
    {
        var norm = workOrderNumber.Trim().ToUpperInvariant();
        return await dbContext.WorkOrders
            .AsNoTracking()
            .Include(w => w.Assets)
            .Include(w => w.StatusHistory)
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.WorkOrderNumber == norm, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetWorkOrdersAsync(
        Guid tenantId,
        Guid? serviceAccountId,
        Guid? primarySiteId,
        short? operationalStatus,
        short? priority,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkOrders
            .AsNoTracking()
            .Include(w => w.Assets)
            .Include(w => w.StatusHistory)
            .Where(w => w.TenantId == tenantId);

        if (serviceAccountId.HasValue)
        {
            query = query.Where(w => w.ServiceAccountId == serviceAccountId.Value);
        }

        if (primarySiteId.HasValue)
        {
            query = query.Where(w => w.PrimarySiteId == primarySiteId.Value);
        }

        if (operationalStatus.HasValue)
        {
            query = query.Where(w => (short)w.OperationalStatus == operationalStatus.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(w => (short)w.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(w => w.WorkOrderNumber.Contains(trimmed)
                                  || w.Summary.Contains(trimmed));
        }

        return await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid tenantId,
        Guid? serviceAccountId,
        Guid? primarySiteId,
        short? operationalStatus,
        short? priority,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkOrders
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId);

        if (serviceAccountId.HasValue)
        {
            query = query.Where(w => w.ServiceAccountId == serviceAccountId.Value);
        }

        if (primarySiteId.HasValue)
        {
            query = query.Where(w => w.PrimarySiteId == primarySiteId.Value);
        }

        if (operationalStatus.HasValue)
        {
            query = query.Where(w => (short)w.OperationalStatus == operationalStatus.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(w => (short)w.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(w => w.WorkOrderNumber.Contains(trimmed)
                                  || w.Summary.Contains(trimmed));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrders.AddAsync(workOrder, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default)
    {
        var norm = workOrderNumber.Trim().ToUpperInvariant();
        return await dbContext.WorkOrders
            .AnyAsync(w => w.TenantId == tenantId && w.WorkOrderNumber == norm, cancellationToken);
    }
}

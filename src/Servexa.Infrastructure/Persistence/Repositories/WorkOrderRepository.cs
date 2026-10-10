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
            .Include(w => w.ScopeItems)
            .Include(w => w.CompletionEvaluations)
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
            .Include(w => w.ScopeItems)
            .Include(w => w.CompletionEvaluations)
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
            .Include(w => w.ScopeItems)
            .Include(w => w.CompletionEvaluations)
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

    public async Task<WorkOrderScopeItem?> GetScopeItemByIdAsync(Guid tenantId, Guid workOrderId, Guid scopeItemId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrderScopeItems
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.WorkOrderId == workOrderId && s.Id == scopeItemId, cancellationToken);
    }

    public async Task AddScopeItemAsync(WorkOrderScopeItem scopeItem, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrderScopeItems.AddAsync(scopeItem, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderCompletionEvaluation>> GetCompletionEvaluationsAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrderCompletionEvaluations
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.WorkOrderId == workOrderId)
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderCompletionEvaluation?> GetLatestCompletionEvaluationAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrderCompletionEvaluations
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.WorkOrderId == workOrderId)
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddCompletionEvaluationAsync(WorkOrderCompletionEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await dbContext.WorkOrderCompletionEvaluations.AddAsync(evaluation, cancellationToken);
    }
}

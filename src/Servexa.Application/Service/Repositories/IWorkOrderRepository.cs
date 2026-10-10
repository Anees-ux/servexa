using Servexa.Domain.Service.Entities;

namespace Servexa.Application.Service.Repositories;

public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetByNumberAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrder>> GetWorkOrdersAsync(
        Guid tenantId,
        Guid? serviceAccountId,
        Guid? primarySiteId,
        short? operationalStatus,
        short? priority,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(
        Guid tenantId,
        Guid? serviceAccountId,
        Guid? primarySiteId,
        short? operationalStatus,
        short? priority,
        string? search,
        CancellationToken cancellationToken = default);
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default);
    Task<WorkOrderScopeItem?> GetScopeItemByIdAsync(Guid tenantId, Guid workOrderId, Guid scopeItemId, CancellationToken cancellationToken = default);
    Task AddScopeItemAsync(WorkOrderScopeItem scopeItem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderCompletionEvaluation>> GetCompletionEvaluationsAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default);
    Task<WorkOrderCompletionEvaluation?> GetLatestCompletionEvaluationAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default);
    Task AddCompletionEvaluationAsync(WorkOrderCompletionEvaluation evaluation, CancellationToken cancellationToken = default);
}

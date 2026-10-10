using Servexa.Application.Service.Dtos;
using Servexa.Domain.Service.Entities;

namespace Servexa.Application.Service.Services;

public interface IWorkOrderCompletionEngine
{
    Task<WorkOrderCompletionReadinessDto> EvaluateAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid? evaluatingUserId,
        CancellationToken cancellationToken = default);

    Task<WorkOrderCompletionEvaluation> CreateEvaluationRecordAsync(
        Guid tenantId,
        Guid workOrderId,
        Guid commandId,
        Guid? evaluatingUserId,
        Guid? triggerBookingId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);
}

using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Services;

namespace Servexa.Application.Service.Queries.EvaluateWorkOrderCompletion;

public sealed record EvaluateWorkOrderCompletionQuery(Guid WorkOrderId) : IRequest<WorkOrderCompletionReadinessDto>;

public sealed class EvaluateWorkOrderCompletionQueryHandler(
    IWorkOrderCompletionEngine completionEngine,
    ITenantContext tenantContext) : IRequestHandler<EvaluateWorkOrderCompletionQuery, WorkOrderCompletionReadinessDto>
{
    public async Task<WorkOrderCompletionReadinessDto> Handle(
        EvaluateWorkOrderCompletionQuery request,
        CancellationToken cancellationToken)
    {
        return await completionEngine.EvaluateAsync(
            tenantContext.TenantId,
            request.WorkOrderId,
            tenantContext.UserId,
            cancellationToken);
    }
}

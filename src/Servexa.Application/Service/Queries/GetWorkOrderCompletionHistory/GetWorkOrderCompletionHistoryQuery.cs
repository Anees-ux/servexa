using System.Text.Json;
using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;

namespace Servexa.Application.Service.Queries.GetWorkOrderCompletionHistory;

public sealed record GetWorkOrderCompletionHistoryQuery(Guid WorkOrderId) : IRequest<IReadOnlyList<WorkOrderCompletionEvaluationDto>>;

public sealed class GetWorkOrderCompletionHistoryQueryHandler(
    IWorkOrderRepository workOrderRepository,
    ITenantContext tenantContext) : IRequestHandler<GetWorkOrderCompletionHistoryQuery, IReadOnlyList<WorkOrderCompletionEvaluationDto>>
{
    public async Task<IReadOnlyList<WorkOrderCompletionEvaluationDto>> Handle(
        GetWorkOrderCompletionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var evals = await workOrderRepository.GetCompletionEvaluationsAsync(
            tenantContext.TenantId,
            request.WorkOrderId,
            cancellationToken);

        return evals.Select(e =>
        {
            IReadOnlyList<CompletionGateResultDto> gates = [];
            try
            {
                gates = JsonSerializer.Deserialize<List<CompletionGateResultDto>>(e.GateResultsJson) ?? [];
            }
            catch
            {
            }

            return new WorkOrderCompletionEvaluationDto(
                e.Id,
                e.WorkOrderId,
                e.CommandId,
                e.EvaluatedAtUtc,
                e.EvaluatedByUserId,
                e.Outcome.ToString(),
                (short)e.Outcome,
                e.IsEligibleForCompletion,
                gates,
                e.TriggerBookingId,
                e.Summary,
                e.Notes);
        }).ToList();
    }
}

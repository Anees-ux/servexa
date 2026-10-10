using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Repositories;

namespace Servexa.Application.Field.Queries.GetWorkTasks;

public sealed record GetWorkTasksQuery(
    Guid WorkOrderId,
    Guid? AssignmentId = null,
    Guid? AssetId = null) : IRequest<IReadOnlyList<WorkTaskDto>>;

public sealed class GetWorkTasksQueryHandler(
    IWorkTaskRepository workTaskRepository,
    ITenantContext tenantContext) : IRequestHandler<GetWorkTasksQuery, IReadOnlyList<WorkTaskDto>>
{
    public async Task<IReadOnlyList<WorkTaskDto>> Handle(GetWorkTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await workTaskRepository.GetByWorkOrderIdAsync(
            tenantContext.TenantId,
            request.WorkOrderId,
            request.AssignmentId,
            request.AssetId,
            asNoTracking: true,
            cancellationToken);

        return tasks.Select(t => new WorkTaskDto(
            t.Id,
            t.WorkOrderId,
            t.WorkOrderScopeItemId,
            t.BookingId,
            t.AssignmentId,
            t.AssetId,
            t.Sequence,
            t.TaskType.ToString(),
            (short)t.TaskType,
            t.Title,
            t.Description,
            t.Status.ToString(),
            (short)t.Status,
            t.IsRequired,
            t.Gate.ToString(),
            (short)t.Gate,
            t.CompletedAtUtc,
            t.CompletedByUserId,
            t.SkipReason,
            t.CreatedAtUtc,
            t.ModifiedAtUtc)).ToList();
    }
}

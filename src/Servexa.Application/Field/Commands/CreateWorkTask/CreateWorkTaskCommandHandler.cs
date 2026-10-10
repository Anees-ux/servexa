using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Field.Enums;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Field.Commands.CreateWorkTask;

public sealed class CreateWorkTaskCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IWorkTaskRepository workTaskRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(CreateWorkTaskCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var workOrder = await workOrderRepository.GetByIdAsync(
            tenantId,
            request.WorkOrderId,
            asNoTracking: false,
            cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' was not found.");

        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete ||
            workOrder.OperationalStatus == WorkOrderOperationalStatus.Cancelled)
        {
            throw new ConflictException($"Cannot add tasks to work order in '{workOrder.OperationalStatus}' status.");
        }

        // FIE-011: Multi-asset attribution invariant verification
        if (request.AssetId.HasValue)
        {
            var isAttached = workOrder.Assets.Any(a =>
                a.AssetId == request.AssetId.Value &&
                a.Status == WorkOrderAssetStatus.InScope);

            if (!isAttached)
            {
                throw new ConflictException(
                    $"Asset '{request.AssetId.Value}' is not associated with Work Order '{workOrder.WorkOrderNumber}'. Tasks must reference an asset within the work order scope.");
            }
        }

        var task = new WorkTask(
            tenantId,
            request.WorkOrderId,
            request.Sequence,
            request.Title,
            (WorkTaskType)request.TaskType,
            request.IsRequired,
            (WorkTaskGate)request.Gate,
            request.Description,
            request.AssetId,
            request.AssignmentId,
            request.BookingId,
            request.WorkOrderScopeItemId);

        await workTaskRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkTaskDto(
            task.Id,
            task.WorkOrderId,
            task.WorkOrderScopeItemId,
            task.BookingId,
            task.AssignmentId,
            task.AssetId,
            task.Sequence,
            task.TaskType.ToString(),
            (short)task.TaskType,
            task.Title,
            task.Description,
            task.Status.ToString(),
            (short)task.Status,
            task.IsRequired,
            task.Gate.ToString(),
            (short)task.Gate,
            task.CompletedAtUtc,
            task.CompletedByUserId,
            task.SkipReason,
            task.CreatedAtUtc,
            task.ModifiedAtUtc);
    }
}

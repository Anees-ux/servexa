using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Field.Enums;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Field.Commands.UpdateWorkTaskStatus;

public sealed class UpdateWorkTaskStatusCommandHandler(
    IWorkTaskRepository workTaskRepository,
    IWorkOrderRepository workOrderRepository,
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<UpdateWorkTaskStatusCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(UpdateWorkTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("User context is required.");

        var isManager = tenantContext.HasPermission(Capabilities.WorkOrderCreate);
        var isTechnician = tenantContext.HasPermission(Capabilities.TechnicianExecute);

        if (!isManager && !isTechnician)
        {
            throw new ForbiddenAccessException("User lacks permission to update task status.");
        }

        var workOrder = await workOrderRepository.GetByIdAsync(
            tenantId,
            request.WorkOrderId,
            asNoTracking: false,
            cancellationToken)
            ?? throw new NotFoundException($"Work order '{request.WorkOrderId}' was not found.");

        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete ||
            workOrder.OperationalStatus == WorkOrderOperationalStatus.Cancelled)
        {
            throw new ConflictException($"Cannot update tasks when work order is '{workOrder.OperationalStatus}'.");
        }

        var task = await workTaskRepository.GetByIdAsync(
            tenantId,
            request.TaskId,
            asNoTracking: false,
            cancellationToken)
            ?? throw new NotFoundException($"Work task '{request.TaskId}' was not found.");

        if (task.WorkOrderId != request.WorkOrderId)
        {
            throw new NotFoundException($"Work task '{request.TaskId}' does not belong to work order '{request.WorkOrderId}'.");
        }

        if (!isManager)
        {
            var resource = await resourceRepository.GetByUserIdAsync(tenantId, userId, cancellationToken);
            if (resource is null)
            {
                throw new ForbiddenAccessException("Authenticated user is not registered as an active field resource.");
            }

            var bookings = await bookingRepository.GetBookingsForResourceAsync(tenantId, resource.Id, cancellationToken: cancellationToken);
            var isAssignedToWorkOrder = bookings.Any(b => b.WorkOrderId == request.WorkOrderId);
            if (!isAssignedToWorkOrder)
            {
                throw new ForbiddenAccessException("Technician is not assigned to this work order.");
            }

            if (task.AssignmentId.HasValue)
            {
                var isAssignedToTask = bookings.Any(b =>
                    b.WorkOrderId == request.WorkOrderId &&
                    b.Assignments.Any(a => a.Id == task.AssignmentId.Value && a.ResourceId == resource.Id));

                if (!isAssignedToTask)
                {
                    throw new ForbiddenAccessException("Technician is not assigned to this task.");
                }
            }
            else if (task.BookingId.HasValue)
            {
                var isAssignedToBooking = bookings.Any(b => b.Id == task.BookingId.Value);
                if (!isAssignedToBooking)
                {
                    throw new ForbiddenAccessException("Technician is not assigned to this booking.");
                }
            }
        }

        var newStatus = (WorkTaskStatus)request.NewStatus;

        switch (newStatus)
        {
            case WorkTaskStatus.InProgress:
                task.Start(userId);
                break;

            case WorkTaskStatus.Completed:
                task.Complete(userId, request.CompletedAtUtc);
                break;

            case WorkTaskStatus.Skipped:
                task.Skip(request.SkipReason ?? string.Empty, userId);
                break;

            case WorkTaskStatus.Pending:
                task.Reset(userId);
                break;

            default:
                throw new ConflictException($"Unsupported task status transition to '{newStatus}'.");
        }

        await workTaskRepository.UpdateAsync(task, cancellationToken);
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

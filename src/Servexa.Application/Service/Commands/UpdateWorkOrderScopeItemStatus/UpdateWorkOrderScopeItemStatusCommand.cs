using FluentValidation;
using MediatR;
using Servexa.Application.Exceptions;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.UpdateWorkOrderScopeItemStatus;

public sealed record UpdateWorkOrderScopeItemStatusCommand(
    Guid WorkOrderId,
    Guid ScopeItemId,
    WorkOrderScopeItemStatus Status) : IRequest<WorkOrderScopeItemDto>;

public sealed class UpdateWorkOrderScopeItemStatusCommandValidator : AbstractValidator<UpdateWorkOrderScopeItemStatusCommand>
{
    public UpdateWorkOrderScopeItemStatusCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("WorkOrderId is required.");

        RuleFor(x => x.ScopeItemId)
            .NotEmpty().WithMessage("ScopeItemId is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Valid scope item status is required.");
    }
}

public sealed class UpdateWorkOrderScopeItemStatusCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<UpdateWorkOrderScopeItemStatusCommand, WorkOrderScopeItemDto>
{
    public async Task<WorkOrderScopeItemDto> Handle(UpdateWorkOrderScopeItemStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("User context is required.");

        if (!tenantContext.HasPermission(Capabilities.WorkOrderCreate) &&
            !tenantContext.HasPermission(Capabilities.TechnicianExecute))
        {
            throw new ForbiddenAccessException("User lacks permission to update scope item status.");
        }

        _ = await workOrderRepository.GetByIdAsync(tenantId, request.WorkOrderId, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException($"Work order with ID '{request.WorkOrderId}' was not found.");

        var scopeItem = await workOrderRepository.GetScopeItemByIdAsync(tenantId, request.WorkOrderId, request.ScopeItemId, cancellationToken)
            ?? throw new NotFoundException($"Scope item with ID '{request.ScopeItemId}' was not found.");

        switch (request.Status)
        {
            case WorkOrderScopeItemStatus.Fulfilled:
                scopeItem.Fulfill(userId);
                break;
            case WorkOrderScopeItemStatus.NotRequired:
                scopeItem.MarkNotRequired(userId);
                break;
            case WorkOrderScopeItemStatus.Cancelled:
                scopeItem.Cancel(userId);
                break;
            default:
                throw new ArgumentException($"Cannot manually transition scope item to '{request.Status}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkOrderScopeItemDto(
            scopeItem.Id,
            scopeItem.WorkOrderId,
            scopeItem.Sequence,
            scopeItem.ScopeType.ToString(),
            (short)scopeItem.ScopeType,
            scopeItem.Description,
            scopeItem.Status.ToString(),
            (short)scopeItem.Status,
            scopeItem.IsRequiredForCompletion,
            scopeItem.AssetId,
            scopeItem.FulfilledAtUtc,
            scopeItem.FulfilledByUserId,
            scopeItem.CreatedAtUtc,
            scopeItem.ModifiedAtUtc);
    }
}

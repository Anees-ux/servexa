using FluentValidation;
using MediatR;
using Servexa.Application.Exceptions;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.AddWorkOrderScopeItem;

public sealed record AddWorkOrderScopeItemCommand(
    Guid WorkOrderId,
    string Description,
    int Sequence = 1,
    WorkOrderScopeType ScopeType = WorkOrderScopeType.Original,
    bool IsRequiredForCompletion = true,
    Guid? AssetId = null) : IRequest<WorkOrderScopeItemDto>;

public sealed class AddWorkOrderScopeItemCommandValidator : AbstractValidator<AddWorkOrderScopeItemCommand>
{
    public AddWorkOrderScopeItemCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("WorkOrderId is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Sequence)
            .GreaterThan(0).WithMessage("Sequence must be positive.");
    }
}

public sealed class AddWorkOrderScopeItemCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<AddWorkOrderScopeItemCommand, WorkOrderScopeItemDto>
{
    public async Task<WorkOrderScopeItemDto> Handle(AddWorkOrderScopeItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        if (!tenantContext.HasPermission(Capabilities.WorkOrderCreate))
        {
            throw new ForbiddenAccessException($"User lacks permission '{Capabilities.WorkOrderCreate}' to manage scope items.");
        }

        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, request.WorkOrderId, asNoTracking: false, cancellationToken)
            ?? throw new NotFoundException($"Work order with ID '{request.WorkOrderId}' was not found.");

        var scopeItem = new WorkOrderScopeItem(
            tenantId: tenantId,
            workOrderId: workOrder.Id,
            sequence: request.Sequence,
            scopeType: request.ScopeType,
            description: request.Description,
            isRequiredForCompletion: request.IsRequiredForCompletion,
            assetId: request.AssetId);

        workOrder.AddScopeItem(scopeItem);
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

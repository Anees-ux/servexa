using MediatR;
using Servexa.Application.Service.Dtos;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.CreateWorkOrder;

public sealed record CreateWorkOrderCommand(
    string? WorkOrderNumber,
    Guid? ServiceRequestId,
    Guid ServiceAccountId,
    Guid? BillToAccountId,
    Guid PrimarySiteId,
    string WorkTypeCode,
    WorkOrderPriority Priority,
    string Summary,
    string? Description = null,
    Guid? PrimaryAssetId = null) : IRequest<WorkOrderDto>;

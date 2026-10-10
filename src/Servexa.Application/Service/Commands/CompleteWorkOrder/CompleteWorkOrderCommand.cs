using MediatR;
using Servexa.Application.Service.Dtos;

namespace Servexa.Application.Service.Commands.CompleteWorkOrder;

public sealed record CompleteWorkOrderCommand(
    Guid WorkOrderId,
    Guid CommandId,
    string? Notes = null) : IRequest<WorkOrderDto>;

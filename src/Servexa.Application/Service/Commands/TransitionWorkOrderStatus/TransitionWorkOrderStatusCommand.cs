using MediatR;
using Servexa.Application.Service.Dtos;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.TransitionWorkOrderStatus;

public sealed record TransitionWorkOrderStatusCommand(
    Guid WorkOrderId,
    WorkOrderOperationalStatus TargetStatus,
    string? PauseReasonCode = null,
    string? PauseNote = null,
    string? Reason = null) : IRequest<WorkOrderDto>;

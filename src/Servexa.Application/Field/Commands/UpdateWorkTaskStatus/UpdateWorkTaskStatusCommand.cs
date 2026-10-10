using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.UpdateWorkTaskStatus;

public record UpdateWorkTaskStatusCommand(
    Guid WorkOrderId,
    Guid TaskId,
    short NewStatus,
    string? SkipReason = null,
    DateTime? CompletedAtUtc = null) : IRequest<WorkTaskDto>;

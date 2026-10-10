using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.CreateWorkTask;

public record CreateWorkTaskCommand(
    Guid WorkOrderId,
    string Title,
    string? Description = null,
    short TaskType = 1,
    int Sequence = 0,
    bool IsRequired = true,
    short Gate = 3,
    Guid? AssetId = null,
    Guid? AssignmentId = null,
    Guid? BookingId = null,
    Guid? WorkOrderScopeItemId = null) : IRequest<WorkTaskDto>;

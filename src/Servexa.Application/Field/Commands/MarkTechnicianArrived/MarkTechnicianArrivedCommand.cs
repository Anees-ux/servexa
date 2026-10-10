using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.MarkTechnicianArrived;

public sealed record MarkTechnicianArrivedCommand(Guid AssignmentId) : IRequest<ExecutionSessionDto>;

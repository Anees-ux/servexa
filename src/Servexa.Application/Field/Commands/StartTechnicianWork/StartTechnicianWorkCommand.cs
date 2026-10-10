using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.StartTechnicianWork;

public sealed record StartTechnicianWorkCommand(Guid AssignmentId) : IRequest<ExecutionSessionDto>;

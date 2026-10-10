using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.ResumeTechnicianWork;

public sealed record ResumeTechnicianWorkCommand(Guid AssignmentId) : IRequest<ExecutionSessionDto>;

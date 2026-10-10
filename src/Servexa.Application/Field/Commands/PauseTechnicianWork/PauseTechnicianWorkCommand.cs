using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.PauseTechnicianWork;

public sealed record PauseTechnicianWorkCommand(
    Guid AssignmentId,
    string Reason) : IRequest<ExecutionSessionDto>;

using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.CompleteTechnicianExecution;

public sealed record CompleteTechnicianExecutionCommand(
    Guid AssignmentId,
    string WorkSummary) : IRequest<ExecutionSessionDto>;

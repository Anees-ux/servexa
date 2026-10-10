using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Commands.StartTechnicianTravel;

public sealed record StartTechnicianTravelCommand(Guid AssignmentId) : IRequest<ExecutionSessionDto>;

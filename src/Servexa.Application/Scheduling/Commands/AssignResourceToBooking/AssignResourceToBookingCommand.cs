using MediatR;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Application.Scheduling.Commands.AssignResourceToBooking;

public sealed record AssignResourceToBookingCommand(
    Guid BookingId,
    Guid ResourceId,
    AssignmentRole AssignmentRole = AssignmentRole.Lead,
    string? SelectionRationale = null) : IRequest<BookingDto>;

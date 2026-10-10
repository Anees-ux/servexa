using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Commands.UnassignResourceFromBooking;

public sealed record UnassignResourceFromBookingCommand(
    Guid BookingId,
    Guid AssignmentId,
    string Reason) : IRequest<BookingDto>;

using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Commands.CancelBooking;

public sealed record CancelBookingCommand(
    Guid BookingId,
    string Reason) : IRequest<BookingDto>;

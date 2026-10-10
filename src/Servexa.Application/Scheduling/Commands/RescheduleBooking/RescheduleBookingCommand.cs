using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Commands.RescheduleBooking;

public sealed record RescheduleBookingCommand(
    Guid BookingId,
    DateTime NewStartUtc,
    DateTime NewEndUtc,
    string Reason,
    string? Initiator = "Dispatcher") : IRequest<BookingDto>;

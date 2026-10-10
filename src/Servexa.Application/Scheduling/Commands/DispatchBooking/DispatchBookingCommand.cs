using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Commands.DispatchBooking;

public sealed record DispatchBookingCommand(Guid BookingId) : IRequest<BookingDto>;

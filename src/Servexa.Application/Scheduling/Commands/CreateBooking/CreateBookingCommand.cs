using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Commands.CreateBooking;

public sealed record CreateBookingCommand(
    Guid WorkOrderId,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    string? SiteTimeZoneId = "UTC",
    string? SchedulingNotes = null,
    string? BookingNumber = null,
    Guid? PrimaryResourceId = null) : IRequest<BookingDto>;

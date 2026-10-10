using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Queries.GetBookingById;

public sealed record GetBookingByIdQuery(Guid Id) : IRequest<BookingDto>;

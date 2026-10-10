using MediatR;
using Servexa.Application.Common.Models;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Queries.GetBookings;

public sealed record GetBookingsQuery(
    Guid? WorkOrderId = null,
    Guid? SiteId = null,
    short? Status = null,
    short? DispatchStatus = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<BookingDto>>;

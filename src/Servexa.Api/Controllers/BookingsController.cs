using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Scheduling.Commands.AssignResourceToBooking;
using Servexa.Application.Scheduling.Commands.CancelBooking;
using Servexa.Application.Scheduling.Commands.CreateBooking;
using Servexa.Application.Scheduling.Commands.DispatchBooking;
using Servexa.Application.Scheduling.Commands.RescheduleBooking;
using Servexa.Application.Scheduling.Commands.UnassignResourceFromBooking;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Queries.GetBookingById;
using Servexa.Application.Scheduling.Queries.GetBookings;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/bookings")]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [HasPermission(Capabilities.BookingCreate)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        [FromBody] CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBookingById), new { id = result.Id }, result);
    }

    [HttpGet]
    [HasPermission(Capabilities.BookingView)]
    [ProducesResponseType(typeof(PagedResult<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BookingDto>>> GetBookings(
        [FromQuery] Guid? workOrderId,
        [FromQuery] Guid? siteId,
        [FromQuery] short? status,
        [FromQuery] short? dispatchStatus,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetBookingsQuery(workOrderId, siteId, status, dispatchStatus, fromUtc, toUtc, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Capabilities.BookingView)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBookingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBookingByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reschedule")]
    [HasPermission(Capabilities.BookingCreate)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> RescheduleBooking(
        Guid id,
        [FromBody] RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RescheduleBookingCommand(id, request.NewStartUtc, request.NewEndUtc, request.Reason, request.Initiator ?? "Dispatcher");
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Capabilities.BookingCreate)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> CancelBooking(
        Guid id,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelBookingCommand(id, request.Reason);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/assignments")]
    [HasPermission(Capabilities.BookingAssign)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> AssignResource(
        Guid id,
        [FromBody] AssignResourceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignResourceToBookingCommand(
            id,
            request.ResourceId,
            request.AssignmentRole ?? AssignmentRole.Lead,
            request.SelectionRationale);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
    [HasPermission(Capabilities.BookingAssign)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> UnassignResource(
        Guid id,
        Guid assignmentId,
        [FromQuery] string reason,
        CancellationToken cancellationToken)
    {
        var command = new UnassignResourceFromBookingCommand(id, assignmentId, reason);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/dispatch")]
    [HasPermission(Capabilities.BookingDispatch)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> DispatchBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DispatchBookingCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record RescheduleBookingRequest(
    DateTime NewStartUtc,
    DateTime NewEndUtc,
    string Reason,
    string? Initiator = "Dispatcher");

public sealed record CancelBookingRequest(string Reason);

public sealed record AssignResourceRequest(
    Guid ResourceId,
    AssignmentRole? AssignmentRole = null,
    string? SelectionRationale = null);

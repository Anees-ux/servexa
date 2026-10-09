using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Service.Commands.CreateWorkOrder;
using Servexa.Application.Service.Commands.TransitionWorkOrderStatus;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Queries.GetWorkOrderById;
using Servexa.Application.Service.Queries.GetWorkOrders;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Service.Enums;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/work-orders")]
public sealed class WorkOrdersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new work order within the authenticated tenant context.
    /// Requires WorkOrder.Create capability.
    /// </summary>
    [HttpPost]
    [HasPermission(Capabilities.WorkOrderCreate)]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkOrderDto>> CreateWorkOrder(
        [FromBody] CreateWorkOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWorkOrderById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated work orders for the authenticated tenant with optional filtering.
    /// Requires WorkOrder.View capability.
    /// </summary>
    [HttpGet]
    [HasPermission(Capabilities.WorkOrderView)]
    [ProducesResponseType(typeof(PagedResult<WorkOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<WorkOrderDto>>> GetWorkOrders(
        [FromQuery] Guid? serviceAccountId,
        [FromQuery] Guid? primarySiteId,
        [FromQuery] short? operationalStatus,
        [FromQuery] short? priority,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetWorkOrdersQuery(serviceAccountId, primarySiteId, operationalStatus, priority, search, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single work order by its ID within the authenticated tenant context.
    /// Requires WorkOrder.View capability.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Capabilities.WorkOrderView)]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderDto>> GetWorkOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Executes an operational status transition for a work order following the approved lifecycle state machine.
    /// Requires WorkOrder.Create capability.
    /// </summary>
    [HttpPost("{id:guid}/transition")]
    [HasPermission(Capabilities.WorkOrderCreate)]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderDto>> TransitionStatus(
        Guid id,
        [FromBody] TransitionWorkOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransitionWorkOrderStatusCommand(
            id,
            request.TargetStatus,
            request.PauseReasonCode,
            request.PauseNote,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record TransitionWorkOrderStatusRequest(
    WorkOrderOperationalStatus TargetStatus,
    string? PauseReasonCode = null,
    string? PauseNote = null,
    string? Reason = null);

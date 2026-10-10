using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Service.Commands.AddWorkOrderScopeItem;
using Servexa.Application.Service.Commands.CompleteWorkOrder;
using Servexa.Application.Service.Commands.CreateWorkOrder;
using Servexa.Application.Service.Commands.TransitionWorkOrderStatus;
using Servexa.Application.Service.Commands.UpdateWorkOrderScopeItemStatus;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Queries.EvaluateWorkOrderCompletion;
using Servexa.Application.Service.Queries.GetWorkOrderById;
using Servexa.Application.Service.Queries.GetWorkOrderCompletionHistory;
using Servexa.Application.Service.Queries.GetWorkOrders;
using Servexa.Application.Field.Commands.CreateWorkTask;
using Servexa.Application.Field.Commands.UpdateWorkTaskStatus;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Queries.GetWorkTasks;
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

    /// <summary>
    /// Evaluates all completion gates and readiness for a work order.
    /// Requires WorkOrder.View capability.
    /// </summary>
    [HttpGet("{id:guid}/completion-evaluation")]
    [HasPermission(Capabilities.WorkOrderView)]
    [ProducesResponseType(typeof(WorkOrderCompletionReadinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderCompletionReadinessDto>> EvaluateCompletion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new EvaluateWorkOrderCompletionQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves historical append-only completion evaluations for a work order.
    /// Requires WorkOrder.View capability.
    /// </summary>
    [HttpGet("{id:guid}/completion-evaluations")]
    [HasPermission(Capabilities.WorkOrderView)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkOrderCompletionEvaluationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WorkOrderCompletionEvaluationDto>>> GetCompletionEvaluations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkOrderCompletionHistoryQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Executes authorized operational completion of a work order following gate evaluation.
    /// Requires WorkOrder.Complete capability.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Capabilities.WorkOrderComplete)]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkOrderDto>> CompleteWorkOrder(
        Guid id,
        [FromBody] CompleteWorkOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteWorkOrderCommand(
            id,
            request.CommandId ?? Guid.CreateVersion7(),
            request.Notes);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Adds an authorized scope item to the work order.
    /// Requires WorkOrder.Create capability.
    /// </summary>
    [HttpPost("{id:guid}/scope-items")]
    [HasPermission(Capabilities.WorkOrderCreate)]
    [ProducesResponseType(typeof(WorkOrderScopeItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderScopeItemDto>> AddScopeItem(
        Guid id,
        [FromBody] AddScopeItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddWorkOrderScopeItemCommand(
            id,
            request.Description,
            request.Sequence,
            request.ScopeType,
            request.IsRequiredForCompletion,
            request.AssetId);

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWorkOrderById), new { id }, result);
    }

    /// <summary>
    /// Updates the execution status of a work order scope item.
    /// </summary>
    [HttpPatch("{id:guid}/scope-items/{itemId:guid}/status")]
    [HasPermission(Capabilities.WorkOrderCreate, Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(WorkOrderScopeItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderScopeItemDto>> UpdateScopeItemStatus(
        Guid id,
        Guid itemId,
        [FromBody] UpdateScopeItemStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkOrderScopeItemStatusCommand(
            id,
            itemId,
            request.Status);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all tasks, checklists, and inspections for the work order.
    /// </summary>
    [HttpGet("{id:guid}/tasks")]
    [HasPermission(Capabilities.WorkOrderView)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WorkTaskDto>>> GetWorkTasks(
        Guid id,
        [FromQuery] Guid? assignmentId,
        [FromQuery] Guid? assetId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkTasksQuery(id, assignmentId, assetId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a discrete work task, checklist item, or inspection task on the work order.
    /// Supports multi-asset attribution (FIE-011) and completion gating (FIE-003).
    /// </summary>
    [HttpPost("{id:guid}/tasks")]
    [HasPermission(Capabilities.WorkOrderCreate)]
    [ProducesResponseType(typeof(WorkTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkTaskDto>> CreateWorkTask(
        Guid id,
        [FromBody] CreateWorkTaskRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWorkTaskCommand(
            id,
            request.Title,
            request.Description,
            request.TaskType,
            request.Sequence,
            request.IsRequired,
            request.Gate,
            request.AssetId,
            request.AssignmentId,
            request.BookingId,
            request.WorkOrderScopeItemId);

        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetWorkOrderById), new { id }, result);
    }

    /// <summary>
    /// Updates the execution status of a work task, checklist item, or inspection (Start, Complete, Skip).
    /// </summary>
    [HttpPatch("{id:guid}/tasks/{taskId:guid}/status")]
    [HasPermission(Capabilities.WorkOrderCreate, Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(WorkTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkTaskDto>> UpdateWorkTaskStatus(
        Guid id,
        Guid taskId,
        [FromBody] UpdateWorkTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkTaskStatusCommand(
            id,
            taskId,
            request.Status,
            request.SkipReason,
            request.CompletedAtUtc);

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record TransitionWorkOrderStatusRequest(
    WorkOrderOperationalStatus TargetStatus,
    string? PauseReasonCode = null,
    string? PauseNote = null,
    string? Reason = null);

public sealed record CompleteWorkOrderRequest(
    Guid? CommandId = null,
    string? Notes = null);

public sealed record AddScopeItemRequest(
    string Description,
    int Sequence = 1,
    WorkOrderScopeType ScopeType = WorkOrderScopeType.Original,
    bool IsRequiredForCompletion = true,
    Guid? AssetId = null);

public sealed record UpdateScopeItemStatusRequest(
    WorkOrderScopeItemStatus Status);

public sealed record CreateWorkTaskRequest(
    string Title,
    string? Description = null,
    short TaskType = 1,
    int Sequence = 0,
    bool IsRequired = true,
    short Gate = 3,
    Guid? AssetId = null,
    Guid? AssignmentId = null,
    Guid? BookingId = null,
    Guid? WorkOrderScopeItemId = null);

public sealed record UpdateWorkTaskStatusRequest(
    short Status,
    string? SkipReason = null,
    DateTime? CompletedAtUtc = null);

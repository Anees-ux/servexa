using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Field.Commands.CompleteTechnicianExecution;
using Servexa.Application.Field.Commands.MarkTechnicianArrived;
using Servexa.Application.Field.Commands.PauseTechnicianWork;
using Servexa.Application.Field.Commands.ResumeTechnicianWork;
using Servexa.Application.Field.Commands.StartTechnicianTravel;
using Servexa.Application.Field.Commands.StartTechnicianWork;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Queries.GetMyAssignedJobs;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/field")]
public sealed class TechnicianExecutionController(ISender sender) : ControllerBase
{
    [HttpGet("me/assignments")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(IReadOnlyList<AssignedJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AssignedJobDto>>> GetMyAssignedJobs(
        [FromQuery] short? assignmentStatus,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyAssignedJobsQuery(assignmentStatus), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/travel")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> StartTravel(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartTechnicianTravelCommand(assignmentId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/arrive")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> MarkArrived(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkTechnicianArrivedCommand(assignmentId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/start")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> StartWork(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartTechnicianWorkCommand(assignmentId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/pause")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> PauseWork(
        Guid assignmentId,
        [FromBody] PauseWorkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PauseTechnicianWorkCommand(assignmentId, request.Reason), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/resume")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> ResumeWork(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResumeTechnicianWorkCommand(assignmentId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/complete")]
    [HasPermission(Capabilities.TechnicianExecute)]
    [ProducesResponseType(typeof(ExecutionSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExecutionSessionDto>> CompleteExecution(
        Guid assignmentId,
        [FromBody] CompleteExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteTechnicianExecutionCommand(assignmentId, request.WorkSummary), cancellationToken);
        return Ok(result);
    }
}

public sealed record PauseWorkRequest(string Reason);

public sealed record CompleteExecutionRequest(string WorkSummary);

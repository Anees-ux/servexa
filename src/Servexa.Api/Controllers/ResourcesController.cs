using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Queries.GetResourceSchedule;
using Servexa.Application.Scheduling.Queries.GetResources;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/resources")]
public sealed class ResourcesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [HasPermission(Capabilities.ResourceView)]
    [ProducesResponseType(typeof(PagedResult<ResourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ResourceDto>>> GetResources(
        [FromQuery] short? status,
        [FromQuery] short? resourceType,
        [FromQuery] Guid? branchId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetResourcesQuery(status, resourceType, branchId, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}/schedule")]
    [HasPermission(Capabilities.ResourceView)]
    [ProducesResponseType(typeof(ResourceScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceScheduleDto>> GetResourceSchedule(
        Guid id,
        [FromQuery] DateTime startUtc,
        [FromQuery] DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetResourceScheduleQuery(id, startUtc, endUtc),
            cancellationToken);

        return Ok(result);
    }
}

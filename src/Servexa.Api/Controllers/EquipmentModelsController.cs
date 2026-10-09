using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Assets.Commands.CreateEquipmentModel;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Queries.GetEquipmentModels;
using Servexa.Application.Common.Models;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/equipment-models")]
public sealed class EquipmentModelsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new equipment model in the tenant registry.
    /// Requires Asset.Create capability.
    /// </summary>
    [HttpPost]
    [HasPermission(Capabilities.AssetCreate)]
    [ProducesResponseType(typeof(EquipmentModelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipmentModelDto>> CreateEquipmentModel(
        [FromBody] CreateEquipmentModelCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEquipmentModels), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated equipment models for the authenticated tenant.
    /// Requires Asset.View capability.
    /// </summary>
    [HttpGet]
    [HasPermission(Capabilities.AssetView)]
    [ProducesResponseType(typeof(PagedResult<EquipmentModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EquipmentModelDto>>> GetEquipmentModels(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetEquipmentModelsQuery(search, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }
}

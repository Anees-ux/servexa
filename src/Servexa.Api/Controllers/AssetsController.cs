using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Assets.Commands.ChangeAssetStatus;
using Servexa.Application.Assets.Commands.CreateAsset;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Queries.GetAssetById;
using Servexa.Application.Assets.Queries.GetAssets;
using Servexa.Application.Common.Models;
using Servexa.Domain.Assets.Enums;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/assets")]
public sealed class AssetsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Registers a new asset within the authenticated tenant context.
    /// Requires Asset.Create capability.
    /// </summary>
    [HttpPost]
    [HasPermission(Capabilities.AssetCreate)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> CreateAsset(
        [FromBody] CreateAssetCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAssetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated assets for the authenticated tenant with optional filtering.
    /// Requires Asset.View capability.
    /// </summary>
    [HttpGet]
    [HasPermission(Capabilities.AssetView)]
    [ProducesResponseType(typeof(PagedResult<AssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetAssets(
        [FromQuery] Guid? siteId,
        [FromQuery] Guid? ownerAccountId,
        [FromQuery] Guid? equipmentModelId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAssetsQuery(siteId, ownerAccountId, equipmentModelId, search, pageNumber, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single asset by its ID within the authenticated tenant context.
    /// Requires Asset.View capability.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Capabilities.AssetView)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetAssetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAssetByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the operational status of an asset following approved lifecycle transitions.
    /// Requires Asset.Update capability.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [HasPermission(Capabilities.AssetUpdate)]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> ChangeAssetStatus(
        Guid id,
        [FromBody] ChangeAssetStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAssetStatusCommand(id, request.TargetStatus, request.Reason, request.SiteId);
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public sealed record ChangeAssetStatusRequest(
    AssetStatus TargetStatus,
    string? Reason = null,
    Guid? SiteId = null);

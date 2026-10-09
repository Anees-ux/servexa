using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Commands.CreateSite;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Queries.GetSiteById;
using Servexa.Application.Customers.Queries.GetSites;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/sites")]
public sealed class SitesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new operational site within the authenticated tenant context.
    /// Requires Site.Create capability.
    /// </summary>
    [HttpPost]
    [HasPermission(Capabilities.SiteCreate)]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteDto>> CreateSite(
        [FromBody] CreateSiteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSiteById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated sites for the authenticated tenant, optionally filtered by branch, account, or search term.
    /// Requires Site.View capability.
    /// </summary>
    [HttpGet]
    [HasPermission(Capabilities.SiteView)]
    [ProducesResponseType(typeof(PagedResult<SiteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SiteDto>>> GetSites(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? accountId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetSitesQuery(branchId, accountId, search, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single site by its ID within the authenticated tenant.
    /// Requires Site.View capability.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Capabilities.SiteView)]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> GetSiteById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSiteByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}

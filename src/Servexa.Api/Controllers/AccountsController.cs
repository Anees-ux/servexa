using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Api.Infrastructure.Authorization;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Commands.CreateAccount;
using Servexa.Application.Customers.Dtos;
using Servexa.Application.Customers.Queries.GetAccountById;
using Servexa.Application.Customers.Queries.GetAccounts;
using Servexa.Domain.Platform.Constants;

namespace Servexa.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Creates a new customer account within the authenticated tenant context.
    /// Requires Customer.Create capability.
    /// </summary>
    [HttpPost]
    [HasPermission(Capabilities.CustomerCreate)]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves paginated customer accounts for the authenticated tenant.
    /// Requires Customer.View capability.
    /// </summary>
    [HttpGet]
    [HasPermission(Capabilities.CustomerView)]
    [ProducesResponseType(typeof(PagedResult<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AccountDto>>> GetAccounts(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAccountsQuery(search, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single customer account by its ID within the authenticated tenant.
    /// Requires Customer.View capability.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Capabilities.CustomerView)]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> GetAccountById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}

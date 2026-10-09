using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servexa.Application.Auth.Dtos;
using Servexa.Application.Common.Interfaces;
using Servexa.Infrastructure.Persistence;

namespace Servexa.Api.Controllers;

[ApiController]
[Route("api/v1/dev")]
public sealed class DevelopmentAuthController(
    IWebHostEnvironment environment,
    IDevDataSeeder devDataSeeder,
    ITenantResolutionService tenantResolutionService,
    IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
{
    public sealed record DevTokenRequest(string? UserType = "admin");

    /// <summary>
    /// Issues a signed development JWT access token using the authoritative platform seed user.
    /// This route is strictly unavailable outside the Development environment.
    /// It NEVER accepts client-defined permissions or roles.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthResponseDto>> IssueDevelopmentToken(
        [FromBody] DevTokenRequest? request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        // Ensure dev seed data is present in database
        await devDataSeeder.SeedDevelopmentDataAsync(cancellationToken);

        // Select the requested dev user identity
        var targetUserId = DevDataSeeder.DevAdminUserId;
        var targetTenantId = DevDataSeeder.DevTenantId;

        // Authoritatively resolve membership and permissions from database
        var resolution = await tenantResolutionService.ResolveTenantMembershipAsync(
            targetTenantId,
            targetUserId,
            cancellationToken);

        if (!resolution.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Development Membership Resolution Failed",
                Detail = resolution.FailureReason ?? "Could not resolve active development tenant membership.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var token = jwtTokenGenerator.GenerateToken(
            resolution.TenantId,
            resolution.UserId,
            resolution.Email ?? "operations@servexa.local",
            resolution.DisplayName ?? "Acme Operations Admin",
            resolution.Roles,
            resolution.Permissions);

        return Ok(new AuthResponseDto(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: 3600,
            TenantId: resolution.TenantId,
            UserId: resolution.UserId,
            Email: resolution.Email ?? "operations@servexa.local",
            DisplayName: resolution.DisplayName ?? "Acme Operations Admin",
            Roles: resolution.Roles,
            Permissions: resolution.Permissions));
    }
}

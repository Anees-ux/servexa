using MediatR;
using Servexa.Application.Auth.Dtos;
using Servexa.Application.Common.Interfaces;

namespace Servexa.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(
    ITenantContext tenantContext) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CurrentUserDto(
            TenantId: tenantContext.TenantId,
            UserId: tenantContext.UserId,
            Email: tenantContext.UserEmail,
            DisplayName: tenantContext.DisplayName,
            Roles: tenantContext.Roles,
            Permissions: tenantContext.Permissions,
            IsAuthenticated: tenantContext.IsAuthenticated));
    }
}

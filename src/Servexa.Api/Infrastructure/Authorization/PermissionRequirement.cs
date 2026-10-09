using Microsoft.AspNetCore.Authorization;

namespace Servexa.Api.Infrastructure.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

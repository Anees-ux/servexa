using Microsoft.AspNetCore.Authorization;

namespace Servexa.Api.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

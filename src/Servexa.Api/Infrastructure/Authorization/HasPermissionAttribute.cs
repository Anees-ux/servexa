using Microsoft.AspNetCore.Authorization;

namespace Servexa.Api.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyOrDelimiter = "|";

    public HasPermissionAttribute(string permission)
        : base(policy: permission)
    {
        Permission = permission;
    }

    public HasPermissionAttribute(params string[] permissions)
        : base(policy: string.Join(PolicyOrDelimiter, permissions))
    {
        Permission = string.Join(PolicyOrDelimiter, permissions);
    }

    public string Permission { get; }
}

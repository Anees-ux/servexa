using System.Security.Claims;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Security;

namespace Servexa.Api.Infrastructure;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private Guid? _ambientTenantId;
    private Guid? _ambientUserId;

    public void SetAmbientTenant(Guid tenantId, Guid? userId = null)
    {
        _ambientTenantId = tenantId;
        _ambientUserId = userId;
    }

    public Guid TenantId
    {
        get
        {
            if (_ambientTenantId.HasValue)
            {
                return _ambientTenantId.Value;
            }

            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = user.FindFirst(ServexaClaims.TenantId)?.Value
                                  ?? user.FindFirst("tenant_id")?.Value;

                if (!string.IsNullOrWhiteSpace(tenantClaim) && Guid.TryParse(tenantClaim, out var parsed))
                {
                    return parsed;
                }
            }

            throw new UnauthorizedAccessException("Tenant context requires a validated authenticated session.");
        }
    }

    public Guid? UserId
    {
        get
        {
            if (_ambientUserId.HasValue)
            {
                return _ambientUserId.Value;
            }

            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var idClaim = user.FindFirst(ServexaClaims.UserId)?.Value
                              ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(idClaim) && Guid.TryParse(idClaim, out var parsed))
                {
                    return parsed;
                }
            }

            return null;
        }
    }

    public string? UserEmail
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                   ?? user?.FindFirst("email")?.Value;
        }
    }

    public string? DisplayName
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Name)?.Value
                   ?? user?.FindFirst("name")?.Value;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return [];
            }

            return user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }
    }

    public IReadOnlyList<string> Permissions
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return [];
            }

            return user.FindAll(ServexaClaims.Permission)
                .Select(c => c.Value)
                .Distinct()
                .ToList();
        }
    }

    public bool IsAuthenticated =>
        _ambientTenantId.HasValue || (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true);

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}

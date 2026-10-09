namespace Servexa.Application.Common.Interfaces;

/// <summary>
/// Server-authoritative ambient tenant and identity context for the executing request.
/// Tenant isolation boundary: TenantId is derived strictly from validated credentials.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? DisplayName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}

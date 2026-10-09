namespace Servexa.Application.Auth.Dtos;

public sealed record CurrentUserDto(
    Guid TenantId,
    Guid? UserId,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsAuthenticated);

namespace Servexa.Application.Auth.Dtos;

public sealed record AuthResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    Guid TenantId,
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

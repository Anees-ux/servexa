namespace Servexa.Application.Common.Interfaces;

/// <summary>
/// Issues signed JWT tokens containing verified tenant and identity claims.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(
        Guid tenantId,
        Guid userId,
        string email,
        string displayName,
        IEnumerable<string> roles,
        IEnumerable<string>? permissions = null);
}

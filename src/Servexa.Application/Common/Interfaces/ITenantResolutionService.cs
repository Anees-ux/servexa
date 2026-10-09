namespace Servexa.Application.Common.Interfaces;

public interface ITenantResolutionService
{
    Task<TenantResolutionResult> ResolveTenantMembershipAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TenantResolutionResult> ResolveExternalIdentityAsync(
        string externalIssuer,
        string externalSubject,
        Guid? requestedTenantId = null,
        CancellationToken cancellationToken = default);
}

public sealed record TenantResolutionResult(
    bool IsSuccess,
    Guid TenantId,
    Guid UserId,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? FailureReason = null);

using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// ScopeAssignment defines the contextual scope constraint for a RoleAssignment.
/// Targets are polymorphic (Tenant, Branch, Territory, Account, Site).
/// </summary>
public class ScopeAssignment
{
    // Parameterless constructor for EF Core instantiation
    private ScopeAssignment()
    {
    }

    public ScopeAssignment(
        Guid tenantId,
        Guid roleAssignmentId,
        ScopeType scopeType,
        Guid? scopeId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (roleAssignmentId == Guid.Empty)
        {
            throw new ArgumentException("RoleAssignmentId cannot be empty.", nameof(roleAssignmentId));
        }

        // ScopeId cannot be empty if specified
        if (scopeId.HasValue && scopeId.Value == Guid.Empty)
        {
            throw new ArgumentException("ScopeId cannot be empty when specified.", nameof(scopeId));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        RoleAssignmentId = roleAssignmentId;
        ScopeType = scopeType;
        ScopeId = scopeId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RoleAssignmentId { get; private set; }
    public ScopeType ScopeType { get; private set; }
    public Guid? ScopeId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

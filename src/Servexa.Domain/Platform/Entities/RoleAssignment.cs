using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// RoleAssignment grants one Role to one TenantUser with contextual scope.
/// </summary>
public class RoleAssignment
{
    private readonly List<ScopeAssignment> _scopes = [];

    // Parameterless constructor for EF Core instantiation
    private RoleAssignment()
    {
    }

    public RoleAssignment(
        Guid tenantId,
        Guid userId,
        Guid roleId,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId cannot be empty.", nameof(roleId));
        }

        if (effectiveToUtc.HasValue && effectiveToUtc.Value < effectiveFromUtc)
        {
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.", nameof(effectiveToUtc));
        }

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public IReadOnlyCollection<ScopeAssignment> Scopes => _scopes.AsReadOnly();

    public void SetEffectiveWindow(DateTime effectiveFromUtc, DateTime? effectiveToUtc)
    {
        if (effectiveToUtc.HasValue && effectiveToUtc.Value < effectiveFromUtc)
        {
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.", nameof(effectiveToUtc));
        }

        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public ScopeAssignment AddScope(ScopeType scopeType, Guid? scopeId = null)
    {
        var existing = _scopes.FirstOrDefault(s => s.ScopeType == scopeType && s.ScopeId == scopeId);
        if (existing is not null)
        {
            return existing;
        }

        var scope = new ScopeAssignment(TenantId, Id, scopeType, scopeId);
        _scopes.Add(scope);
        ModifiedAtUtc = DateTime.UtcNow;
        return scope;
    }

    public void RemoveScope(Guid scopeAssignmentId)
    {
        var existing = _scopes.FirstOrDefault(s => s.Id == scopeAssignmentId);
        if (existing is not null)
        {
            _scopes.Remove(existing);
            ModifiedAtUtc = DateTime.UtcNow;
        }
    }

    public bool IsActive(DateTime? asOfUtc = null)
    {
        var instant = asOfUtc ?? DateTime.UtcNow;
        return EffectiveFromUtc <= instant && (!EffectiveToUtc.HasValue || EffectiveToUtc.Value >= instant);
    }
}

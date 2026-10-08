using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// Role represents a tenant-scoped bundle of capabilities.
/// A Role holds capabilities only; scope is assigned separately via ScopeAssignment.
/// </summary>
public class Role
{
    private readonly List<RolePermission> _permissions = [];

    // Parameterless constructor for EF Core instantiation
    private Role()
    {
    }

    public Role(
        Guid tenantId,
        string name,
        bool isSystem = false,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        Name = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();
        IsSystem = isSystem;
        Status = RoleStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public bool IsSystem { get; private set; }
    public RoleStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public void UpdateDetails(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = name.Trim().ToUpperInvariant();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(RoleStatus newStatus)
    {
        if (IsSystem && newStatus != RoleStatus.Active)
        {
            throw new InvalidOperationException("System roles cannot be deactivated.");
        }

        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void AddPermission(Guid permissionId)
    {
        if (permissionId == Guid.Empty)
        {
            throw new ArgumentException("PermissionId cannot be empty.", nameof(permissionId));
        }

        if (_permissions.Any(p => p.PermissionId == permissionId))
        {
            return;
        }

        _permissions.Add(new RolePermission(Id, permissionId));
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void RemovePermission(Guid permissionId)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is not null)
        {
            _permissions.Remove(existing);
            ModifiedAtUtc = DateTime.UtcNow;
        }
    }
}

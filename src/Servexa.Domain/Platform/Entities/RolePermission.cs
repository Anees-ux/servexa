namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// RolePermission represents the mapping between a Role and a Permission.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Canonical domain entity named per approved Servexa specifications")]
public class RolePermission
{
    // Parameterless constructor for EF Core instantiation
    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId cannot be empty.", nameof(roleId));
        }

        if (permissionId == Guid.Empty)
        {
            throw new ArgumentException("PermissionId cannot be empty.", nameof(permissionId));
        }

        RoleId = roleId;
        PermissionId = permissionId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

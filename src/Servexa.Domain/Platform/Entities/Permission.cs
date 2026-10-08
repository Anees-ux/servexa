using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// Permission represents a global capability catalogue item (e.g. Booking.Create, WorkOrder.Complete).
/// Permissions are released with the application; tenants cannot invent permissions.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Canonical domain entity named per approved Servexa specifications")]
public class Permission
{
    // Parameterless constructor for EF Core instantiation
    private Permission()
    {
    }

    public Permission(
        string code,
        string description,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Id = id ?? Guid.CreateVersion7();
        Code = code.Trim();
        Description = description.Trim();
        Status = PermissionStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public PermissionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }

    public void UpdateDetails(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Description = description.Trim();
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(PermissionStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

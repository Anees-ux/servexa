using Servexa.Domain.Platform.Enums;

namespace Servexa.Domain.Platform.Entities;

/// <summary>
/// Territory represents a geographic / service-coverage unit within a Tenant.
/// Distinct from Branch (organizational/financial unit).
/// </summary>
public class Territory
{
    // Parameterless constructor for EF Core instantiation
    private Territory()
    {
    }

    public Territory(
        Guid tenantId,
        string code,
        string name,
        Guid? parentTerritoryId = null,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var assignedId = id ?? Guid.CreateVersion7();
        if (parentTerritoryId.HasValue && parentTerritoryId.Value == assignedId)
        {
            throw new ArgumentException("A territory cannot be its own parent.", nameof(parentTerritoryId));
        }

        Id = assignedId;
        TenantId = tenantId;
        Code = code.Trim();
        Name = name.Trim();
        ParentTerritoryId = parentTerritoryId;
        Status = TerritoryStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid? ParentTerritoryId { get; private set; }
    public TerritoryStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateDetails(string name, Guid? parentTerritoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (parentTerritoryId.HasValue && parentTerritoryId.Value == Id)
        {
            throw new ArgumentException("A territory cannot be its own parent.", nameof(parentTerritoryId));
        }

        Name = name.Trim();
        ParentTerritoryId = parentTerritoryId;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStatus(TerritoryStatus newStatus)
    {
        Status = newStatus;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

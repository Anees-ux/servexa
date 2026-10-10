using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Domain.Scheduling.Entities;

/// <summary>
/// Schedulable resource master (Physical Model §12.1, LDM §9.1).
/// Represents human technicians, subcontractors, crews, or schedulable equipment separately from user accounts.
/// </summary>
public sealed class Resource
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ResourceCode { get; private set; } = string.Empty;
    public ResourceType ResourceType { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public Guid? HomeBranchId { get; private set; }
    public bool ExclusiveCapacity { get; private set; } = true;
    public ResourceStatus Status { get; private set; } = ResourceStatus.Active;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Resource() { }

    public Resource(
        Guid tenantId,
        string resourceCode,
        string displayName,
        ResourceType resourceType = ResourceType.Technician,
        Guid? userId = null,
        Guid? homeBranchId = null,
        bool exclusiveCapacity = true,
        ResourceStatus status = ResourceStatus.Active,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ResourceCode = resourceCode.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        ResourceType = resourceType;
        UserId = userId == Guid.Empty ? null : userId;
        HomeBranchId = homeBranchId == Guid.Empty ? null : homeBranchId;
        ExclusiveCapacity = exclusiveCapacity;
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string displayName, ResourceType resourceType, Guid? homeBranchId, bool exclusiveCapacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        ResourceType = resourceType;
        HomeBranchId = homeBranchId == Guid.Empty ? null : homeBranchId;
        ExclusiveCapacity = exclusiveCapacity;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void LinkUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        UserId = userId;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void UnlinkUser()
    {
        UserId = null;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = ResourceStatus.Active;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ResourceStatus.Inactive;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

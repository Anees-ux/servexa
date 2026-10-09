using Servexa.Domain.Assets.Enums;

namespace Servexa.Domain.Assets.Entities;

/// <summary>
/// EquipmentModel represents an equipment catalogue template (manufacturer/model).
/// Not an installed unit and not a stockable product.
/// </summary>
public class EquipmentModel
{
    // Parameterless constructor for EF Core instantiation
    private EquipmentModel()
    {
    }

    public EquipmentModel(
        Guid tenantId,
        string manufacturerName,
        string modelCode,
        string displayName,
        string categoryCode,
        EquipmentTrackingPolicy trackingPolicy = EquipmentTrackingPolicy.Serialized,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryCode);

        Id = id ?? Guid.CreateVersion7();
        TenantId = tenantId;
        ManufacturerName = manufacturerName.Trim();
        ModelCode = modelCode.Trim();
        DisplayName = displayName.Trim();
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        TrackingPolicy = trackingPolicy;
        Status = EquipmentModelStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string ManufacturerName { get; private set; } = null!;
    public string ModelCode { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string CategoryCode { get; private set; } = null!;
    public EquipmentTrackingPolicy TrackingPolicy { get; private set; }
    public EquipmentModelStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ModifiedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void UpdateDetails(string displayName, string categoryCode, EquipmentTrackingPolicy trackingPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryCode);

        DisplayName = displayName.Trim();
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        TrackingPolicy = trackingPolicy;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Retire()
    {
        Status = EquipmentModelStatus.Retired;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = EquipmentModelStatus.Active;
        ModifiedAtUtc = DateTime.UtcNow;
    }
}

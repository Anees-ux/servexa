namespace Servexa.Application.Assets.Dtos;

public sealed record AssetDto(
    Guid Id,
    Guid TenantId,
    string AssetNumber,
    Guid EquipmentModelId,
    string? EquipmentModelName,
    string? ManufacturerName,
    string? ModelCode,
    string? SerialNumber,
    Guid? CurrentSiteId,
    string? CurrentSiteName,
    Guid? CurrentOwnerAccountId,
    string? CurrentOwnerAccountName,
    string Status,
    short StatusValue,
    DateTime? InstalledAtUtc,
    DateTime? DecommissionedAtUtc,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc,
    IReadOnlyList<AssetLifecycleEventDto> LifecycleEvents);

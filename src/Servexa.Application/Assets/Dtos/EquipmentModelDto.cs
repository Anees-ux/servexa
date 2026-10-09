namespace Servexa.Application.Assets.Dtos;

public sealed record EquipmentModelDto(
    Guid Id,
    Guid TenantId,
    string ManufacturerName,
    string ModelCode,
    string DisplayName,
    string CategoryCode,
    string TrackingPolicy,
    short TrackingPolicyValue,
    string Status,
    short StatusValue,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc);

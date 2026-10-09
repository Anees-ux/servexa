namespace Servexa.Application.Service.Dtos;

public sealed record WorkOrderDto(
    Guid Id,
    Guid TenantId,
    string WorkOrderNumber,
    Guid? ServiceRequestId,
    Guid ServiceAccountId,
    string? ServiceAccountName,
    Guid BillToAccountId,
    string? BillToAccountName,
    Guid PrimarySiteId,
    string? PrimarySiteName,
    string WorkTypeCode,
    string Priority,
    short PriorityValue,
    string OperationalStatus,
    short OperationalStatusValue,
    string Summary,
    string? Description,
    string? PauseReasonCode,
    string? PauseNote,
    DateTime? OperationallyCompletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc,
    IReadOnlyList<WorkOrderAssetDto> Assets,
    IReadOnlyList<WorkOrderStatusHistoryDto> StatusHistory);

public sealed record WorkOrderAssetDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? ModelDisplayName,
    string Role,
    short RoleValue,
    Guid SiteIdAtTime,
    string Status,
    short StatusValue);

public sealed record WorkOrderStatusHistoryDto(
    Guid Id,
    string FromStatus,
    short FromStatusValue,
    string ToStatus,
    short ToStatusValue,
    string? PauseReasonCode,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc,
    string? Reason);

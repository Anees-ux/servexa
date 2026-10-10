namespace Servexa.Application.Scheduling.Dtos;

public sealed record BookingStatusHistoryDto(
    Guid Id,
    Guid BookingId,
    string FromStatus,
    short FromStatusValue,
    string ToStatus,
    short ToStatusValue,
    string? Reason,
    string? Trigger,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc);

namespace Servexa.Application.Scheduling.Dtos;

public sealed record BookingScheduleRevisionDto(
    Guid Id,
    Guid BookingId,
    int RevisionNo,
    DateTime PreviousStartUtc,
    DateTime PreviousEndUtc,
    DateTime NewStartUtc,
    DateTime NewEndUtc,
    string? ReasonCode,
    string? Reason,
    string Initiator,
    Guid? ChangedByUserId,
    DateTime CreatedAtUtc);

namespace Servexa.Application.Field.Dtos;

public sealed record ExecutionSessionDto(
    Guid Id,
    Guid ResourceAssignmentId,
    Guid BookingId,
    Guid WorkOrderId,
    Guid ResourceId,
    Guid UserId,
    string Status,
    short StatusValue,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? WorkSummary,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ExecutionIntervalDto> Intervals);

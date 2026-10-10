namespace Servexa.Application.Scheduling.Dtos;

public sealed record ResourceAssignmentDto(
    Guid Id,
    Guid BookingId,
    Guid ResourceId,
    string? ResourceCode,
    string? ResourceDisplayName,
    string AssignmentRole,
    short AssignmentRoleValue,
    DateTime PlannedStartUtc,
    DateTime PlannedEndUtc,
    string Status,
    short StatusValue,
    string? SelectionRationale,
    DateTime? DispatchedAtUtc,
    DateTime? CompletedAtUtc);

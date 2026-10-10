namespace Servexa.Application.Field.Dtos;

public sealed record ExecutionIntervalDto(
    Guid Id,
    Guid ExecutionSessionId,
    string IntervalType,
    short IntervalTypeValue,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? Notes);

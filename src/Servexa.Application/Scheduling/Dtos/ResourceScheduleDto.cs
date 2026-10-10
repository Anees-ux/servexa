namespace Servexa.Application.Scheduling.Dtos;

public sealed record ResourceCommitmentDto(
    Guid Id,
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Kind,
    string Status,
    Guid? BookingId,
    Guid? WorkOrderId);

public sealed record ResourceScheduleDto(
    Guid ResourceId,
    string ResourceCode,
    string DisplayName,
    IReadOnlyCollection<ResourceCommitmentDto> Commitments);

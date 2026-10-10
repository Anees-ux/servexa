namespace Servexa.Application.Field.Dtos;

public record WorkTaskDto(
    Guid Id,
    Guid WorkOrderId,
    Guid? WorkOrderScopeItemId,
    Guid? BookingId,
    Guid? AssignmentId,
    Guid? AssetId,
    int Sequence,
    string TaskType,
    short TaskTypeValue,
    string Title,
    string? Description,
    string Status,
    short StatusValue,
    bool IsRequired,
    string Gate,
    short GateValue,
    DateTime? CompletedAtUtc,
    Guid? CompletedByUserId,
    string? SkipReason,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc);

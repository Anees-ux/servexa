namespace Servexa.Application.Assets.Dtos;

public sealed record AssetLifecycleEventDto(
    Guid Id,
    string EventType,
    short EventTypeValue,
    string? PreviousStatus,
    short? PreviousStatusValue,
    string? NewStatus,
    short? NewStatusValue,
    Guid? FromSiteId,
    Guid? ToSiteId,
    Guid? FromOwnerAccountId,
    Guid? ToOwnerAccountId,
    string? Reason,
    Guid? ActorUserId,
    DateTime OccurredAtUtc,
    DateTime RecordedAtUtc);

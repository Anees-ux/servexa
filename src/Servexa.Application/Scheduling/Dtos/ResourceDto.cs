namespace Servexa.Application.Scheduling.Dtos;

public sealed record ResourceDto(
    Guid Id,
    string ResourceCode,
    string DisplayName,
    string ResourceType,
    short ResourceTypeValue,
    string Status,
    short StatusValue,
    Guid? UserId,
    Guid? HomeBranchId,
    bool ExclusiveCapacity);

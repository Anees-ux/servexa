namespace Servexa.Application.Service.Dtos;

public sealed record WorkOrderScopeItemDto(
    Guid Id,
    Guid WorkOrderId,
    int Sequence,
    string ScopeType,
    short ScopeTypeValue,
    string Description,
    string Status,
    short StatusValue,
    bool IsRequiredForCompletion,
    Guid? AssetId,
    DateTime? FulfilledAtUtc,
    Guid? FulfilledByUserId,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc);

public sealed record CompletionGateResultDto(
    string GateName,
    bool Passed,
    string Description,
    string? BlockingReason);

public sealed record WorkOrderCompletionEvaluationDto(
    Guid Id,
    Guid WorkOrderId,
    Guid CommandId,
    DateTime EvaluatedAtUtc,
    Guid? EvaluatedByUserId,
    string Outcome,
    short OutcomeValue,
    bool IsEligibleForCompletion,
    IReadOnlyList<CompletionGateResultDto> GateResults,
    Guid? TriggerBookingId,
    string? Summary,
    string? Notes);

public sealed record WorkOrderCompletionReadinessDto(
    Guid WorkOrderId,
    bool IsCompleteEligible,
    string CurrentStatus,
    short CurrentStatusValue,
    string RecommendedOutcome,
    short RecommendedOutcomeValue,
    IReadOnlyList<CompletionGateResultDto> Gates,
    IReadOnlyList<string> UnmetRequirements,
    IReadOnlyList<WorkOrderScopeItemDto> ScopeItems,
    int TotalBookingsCount,
    int CompletedBookingsCount,
    int TotalExecutionSessionsCount,
    int CompletedExecutionSessionsCount,
    WorkOrderCompletionEvaluationDto? LatestEvaluation);

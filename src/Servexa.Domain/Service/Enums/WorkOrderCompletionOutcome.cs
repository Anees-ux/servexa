namespace Servexa.Domain.Service.Enums;

/// <summary>
/// Operational outcome produced by a WorkOrderCompletionEvaluation.
/// </summary>
public enum WorkOrderCompletionOutcome : short
{
    OperationallyComplete = 1,
    FollowUpRequired = 2,
    ManagerReviewRequired = 3,
    Blocked = 4,
    NoChange = 5
}

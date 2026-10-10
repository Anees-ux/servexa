using Servexa.Application.Field.Dtos;
using Servexa.Domain.Field.Entities;

namespace Servexa.Application.Field.Mappers;

public static class ExecutionSessionMapper
{
    public static ExecutionSessionDto ToDto(ExecutionSession session)
    {
        var intervalDtos = session.Intervals.Select(i => new ExecutionIntervalDto(
            i.Id,
            i.ExecutionSessionId,
            i.IntervalType.ToString(),
            (short)i.IntervalType,
            i.StartedAtUtc,
            i.EndedAtUtc,
            i.Reason)).ToList();

        return new ExecutionSessionDto(
            session.Id,
            session.ResourceAssignmentId,
            session.BookingId,
            session.WorkOrderId,
            session.ResourceId,
            session.UserId,
            session.Status.ToString(),
            (short)session.Status,
            session.StartedAtUtc,
            session.CompletedAtUtc,
            session.WorkSummary,
            session.CreatedAtUtc,
            intervalDtos);
    }
}

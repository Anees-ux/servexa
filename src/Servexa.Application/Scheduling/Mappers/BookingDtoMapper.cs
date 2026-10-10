using Servexa.Application.Scheduling.Dtos;
using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Application.Scheduling.Mappers;

public static class BookingDtoMapper
{
    public static BookingDto ToDto(
        Booking booking,
        string? workOrderNumber = null,
        string? workOrderSummary = null,
        string? siteName = null,
        Guid? accountId = null,
        string? accountName = null,
        IReadOnlyDictionary<Guid, (string Code, string Name)>? resourceLookup = null)
    {
        var assignments = booking.Assignments.Select(a =>
        {
            string? code = null;
            string? name = null;
            if (resourceLookup != null && resourceLookup.TryGetValue(a.ResourceId, out var resInfo))
            {
                code = resInfo.Code;
                name = resInfo.Name;
            }

            return new ResourceAssignmentDto(
                a.Id,
                a.BookingId,
                a.ResourceId,
                code,
                name,
                a.AssignmentRole.ToString(),
                (short)a.AssignmentRole,
                a.PlannedStartUtc,
                a.PlannedEndUtc,
                a.Status.ToString(),
                (short)a.Status,
                a.SelectionRationale,
                a.DispatchedAtUtc,
                a.CompletedAtUtc);
        }).ToList();

        var revisions = booking.Revisions.Select(r => new BookingScheduleRevisionDto(
            r.Id,
            r.BookingId,
            r.RevisionNo,
            r.PreviousStartUtc,
            r.PreviousEndUtc,
            r.NewStartUtc,
            r.NewEndUtc,
            r.ReasonCode,
            r.Reason,
            r.Initiator,
            r.ChangedByUserId,
            r.ChangedAtUtc)).ToList();

        var history = booking.StatusHistory.Select(h => new BookingStatusHistoryDto(
            h.Id,
            h.BookingId,
            h.FromStatus.ToString(),
            (short)h.FromStatus,
            h.ToStatus.ToString(),
            (short)h.ToStatus,
            h.Reason,
            h.Trigger,
            h.ChangedByUserId,
            h.ChangedAtUtc)).ToList();

        return new BookingDto(
            booking.Id,
            booking.BookingNumber,
            booking.WorkOrderId,
            workOrderNumber,
            workOrderSummary,
            booking.SiteId,
            siteName,
            accountId,
            accountName,
            booking.PlannedStartUtc,
            booking.PlannedEndUtc,
            booking.SiteTimeZoneId,
            booking.Status.ToString(),
            (short)booking.Status,
            booking.DispatchStatus.ToString(),
            (short)booking.DispatchStatus,
            booking.Sequence,
            booking.CancellationReason,
            booking.SchedulingNotes,
            booking.DispatchedAtUtc,
            booking.StartedAtUtc,
            booking.CompletedAtUtc,
            booking.CreatedAtUtc,
            assignments,
            revisions,
            history);
    }
}

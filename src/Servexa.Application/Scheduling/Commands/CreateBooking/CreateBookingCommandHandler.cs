using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Mappers;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Scheduling.Commands.CreateBooking;

public sealed class CreateBookingCommandHandler(
    IBookingRepository bookingRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    INumberSeriesService numberSeriesService,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId;

        // 1. Verify Work Order exists and check operational lifecycle prerequisite
        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, request.WorkOrderId, asNoTracking: false, cancellationToken);
        if (workOrder is null)
        {
            throw new NotFoundException($"Work order with ID '{request.WorkOrderId}' was not found.");
        }

        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.Draft)
        {
            throw new InvalidOperationException("Cannot create a booking for a Draft work order. The work order must be Approved first.");
        }

        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.Cancelled ||
            workOrder.OperationalStatus == WorkOrderOperationalStatus.OperationallyComplete)
        {
            throw new InvalidOperationException($"Cannot schedule work order in status '{workOrder.OperationalStatus}'.");
        }

        // 2. Fetch site and customer context
        var site = await siteRepository.GetByIdAsync(tenantId, workOrder.PrimarySiteId, cancellationToken);
        var siteName = site?.Name;
        var account = await accountRepository.GetByIdAsync(tenantId, workOrder.ServiceAccountId, cancellationToken);

        // 3. Allocate BookingNumber
        string bookingNumber;
        if (string.IsNullOrWhiteSpace(request.BookingNumber))
        {
            bookingNumber = await numberSeriesService.AllocateNextNumberAsync(tenantId, "BOOKING", "BKG-", cancellationToken);
        }
        else
        {
            bookingNumber = request.BookingNumber.Trim().ToUpperInvariant();
            var exists = await bookingRepository.ExistsAsync(tenantId, bookingNumber, cancellationToken);
            if (exists)
            {
                throw new ConflictException($"A booking with number '{bookingNumber}' already exists for this tenant.");
            }
        }

        // 4. Create Booking aggregate
        var booking = new Booking(
            tenantId: tenantId,
            bookingNumber: bookingNumber,
            workOrderId: workOrder.Id,
            siteId: workOrder.PrimarySiteId,
            plannedStartUtc: request.PlannedStartUtc,
            plannedEndUtc: request.PlannedEndUtc,
            siteTimeZoneId: string.IsNullOrWhiteSpace(request.SiteTimeZoneId) ? (site?.TimeZoneId ?? "UTC") : request.SiteTimeZoneId.Trim(),
            status: BookingStatus.Scheduled,
            sequence: 1,
            schedulingNotes: request.SchedulingNotes,
            createdByUserId: userId);

        // 5. If WorkOrder is in Approved status, transition it to Scheduled
        if (workOrder.OperationalStatus == WorkOrderOperationalStatus.Approved)
        {
            workOrder.MarkScheduled(userId);
        }

        Dictionary<Guid, (string Code, string Name)>? resourceLookup = null;

        // 6. Optional primary resource assignment
        if (request.PrimaryResourceId.HasValue)
        {
            var resource = await resourceRepository.GetByIdAsync(tenantId, request.PrimaryResourceId.Value, asNoTracking: false, cancellationToken);
            if (resource is null)
            {
                throw new NotFoundException($"Resource with ID '{request.PrimaryResourceId.Value}' was not found.");
            }
            if (resource.Status != ResourceStatus.Active)
            {
                throw new InvalidOperationException($"Resource '{resource.DisplayName}' is not active and cannot be scheduled.");
            }

            // Concurrency guard and overlap check
            await commitmentRepository.EnsureScheduleGuardAsync(tenantId, resource.Id, cancellationToken);
            var overlaps = await commitmentRepository.GetActiveCommitmentsForResourceAsync(tenantId, resource.Id, request.PlannedStartUtc, request.PlannedEndUtc, cancellationToken);
            if (overlaps.Count > 0)
            {
                var conflictLog = new SchedulingConflictLog(
                    tenantId: tenantId,
                    resourceId: resource.Id,
                    attemptedStartUtc: request.PlannedStartUtc,
                    attemptedEndUtc: request.PlannedEndUtc,
                    blockingCommitmentId: overlaps[0].Id,
                    conflictType: "Overlap",
                    reason: $"Overlapping commitment exists from {overlaps[0].StartUtc:s} to {overlaps[0].EndUtc:s}",
                    attemptedByUserId: userId);
                await commitmentRepository.AddConflictLogAsync(conflictLog, cancellationToken);

                throw new ConflictException($"Resource '{resource.DisplayName}' already has an overlapping commitment ({overlaps[0].StartUtc:g} - {overlaps[0].EndUtc:g}).");
            }

            var assignment = new ResourceAssignment(
                tenantId: tenantId,
                bookingId: booking.Id,
                resourceId: resource.Id,
                plannedStartUtc: request.PlannedStartUtc,
                plannedEndUtc: request.PlannedEndUtc,
                assignmentRole: AssignmentRole.Lead,
                selectionRationale: "Assigned at booking creation");

            booking.AddAssignment(assignment);

            var commitment = new ResourceCommitment(
                tenantId: tenantId,
                resourceId: resource.Id,
                resourceAssignmentId: assignment.Id,
                startUtc: request.PlannedStartUtc,
                endUtc: request.PlannedEndUtc,
                commitmentKind: CommitmentKind.Direct);

            await commitmentRepository.AddAsync(commitment, cancellationToken);
            resourceLookup = new() { [resource.Id] = (resource.ResourceCode, resource.DisplayName) };
        }

        await bookingRepository.AddAsync(booking, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BookingDtoMapper.ToDto(
            booking,
            workOrder.WorkOrderNumber,
            workOrder.Summary,
            siteName,
            account?.Id,
            account?.DisplayName ?? account?.LegalName,
            resourceLookup);
    }
}

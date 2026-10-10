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

namespace Servexa.Application.Scheduling.Commands.AssignResourceToBooking;

public sealed class AssignResourceToBookingCommandHandler(
    IBookingRepository bookingRepository,
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<AssignResourceToBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(AssignResourceToBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var booking = await bookingRepository.GetByIdAsync(tenantId, request.BookingId, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{request.BookingId}' was not found.");
        }

        if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot assign resources to a booking in status '{booking.Status}'.");
        }

        var resource = await resourceRepository.GetByIdAsync(tenantId, request.ResourceId, asNoTracking: false, cancellationToken);
        if (resource is null)
        {
            throw new NotFoundException($"Resource with ID '{request.ResourceId}' was not found.");
        }

        if (resource.Status != ResourceStatus.Active)
        {
            throw new InvalidOperationException($"Resource '{resource.DisplayName}' is inactive and cannot be assigned.");
        }

        // Check if resource is already actively assigned to this booking
        if (booking.Assignments.Any(a => a.ResourceId == resource.Id &&
            a.Status != AssignmentStatus.Cancelled &&
            a.Status != AssignmentStatus.Completed &&
            a.Status != AssignmentStatus.Replaced))
        {
            throw new ConflictException($"Resource '{resource.DisplayName}' is already actively assigned to this booking.");
        }

        // Check if Lead role is already taken
        if (request.AssignmentRole == AssignmentRole.Lead &&
            booking.Assignments.Any(a => a.AssignmentRole == AssignmentRole.Lead &&
                a.Status != AssignmentStatus.Cancelled &&
                a.Status != AssignmentStatus.Replaced))
        {
            throw new ConflictException("A Lead resource is already assigned to this booking. Reassign or assign as Support.");
        }

        // Concurrency guard and overlap check
        await commitmentRepository.EnsureScheduleGuardAsync(tenantId, resource.Id, cancellationToken);

        var overlaps = await commitmentRepository.GetActiveCommitmentsForResourceAsync(
            tenantId,
            resource.Id,
            booking.PlannedStartUtc,
            booking.PlannedEndUtc,
            cancellationToken);

        if (overlaps.Count > 0)
        {
            var conflictLog = new SchedulingConflictLog(
                tenantId: tenantId,
                resourceId: resource.Id,
                attemptedStartUtc: booking.PlannedStartUtc,
                attemptedEndUtc: booking.PlannedEndUtc,
                blockingCommitmentId: overlaps[0].Id,
                conflictType: "Overlap",
                reason: $"Overlapping commitment exists from {overlaps[0].StartUtc:s} to {overlaps[0].EndUtc:s}",
                attemptedByUserId: tenantContext.UserId);
            await commitmentRepository.AddConflictLogAsync(conflictLog, cancellationToken);

            throw new ConflictException($"Resource '{resource.DisplayName}' already has an overlapping commitment ({overlaps[0].StartUtc:g} - {overlaps[0].EndUtc:g}).");
        }

        var assignment = new ResourceAssignment(
            tenantId: tenantId,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: request.AssignmentRole,
            selectionRationale: request.SelectionRationale);

        booking.AddAssignment(assignment);

        var commitment = new ResourceCommitment(
            tenantId: tenantId,
            resourceId: resource.Id,
            resourceAssignmentId: assignment.Id,
            startUtc: booking.PlannedStartUtc,
            endUtc: booking.PlannedEndUtc,
            commitmentKind: CommitmentKind.Direct);

        await commitmentRepository.AddAsync(commitment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var workOrder = await workOrderRepository.GetByIdAsync(tenantId, booking.WorkOrderId, asNoTracking: true, cancellationToken);
        var site = await siteRepository.GetByIdAsync(tenantId, booking.SiteId, cancellationToken);
        var account = workOrder != null ? await accountRepository.GetByIdAsync(tenantId, workOrder.ServiceAccountId, cancellationToken) : null;

        var assignedResourceIds = booking.Assignments.Select(a => a.ResourceId).Distinct().ToList();
        var resources = new Dictionary<Guid, (string Code, string Name)>();
        foreach (var resId in assignedResourceIds)
        {
            var r = await resourceRepository.GetByIdAsync(tenantId, resId, asNoTracking: true, cancellationToken);
            if (r != null) resources[r.Id] = (r.ResourceCode, r.DisplayName);
        }

        return BookingDtoMapper.ToDto(
            booking,
            workOrder?.WorkOrderNumber,
            workOrder?.Summary,
            site?.Name,
            account?.Id,
            account?.DisplayName ?? account?.LegalName,
            resources);
    }
}

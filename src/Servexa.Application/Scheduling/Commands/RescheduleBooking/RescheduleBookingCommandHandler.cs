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

namespace Servexa.Application.Scheduling.Commands.RescheduleBooking;

public sealed class RescheduleBookingCommandHandler(
    IBookingRepository bookingRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<RescheduleBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId;

        var booking = await bookingRepository.GetByIdAsync(tenantId, request.BookingId, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{request.BookingId}' was not found.");
        }

        // Active assignments that will be rescheduled
        var activeAssignments = booking.Assignments
            .Where(a => a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.Dispatched)
            .ToList();

        var existingCommitments = await commitmentRepository.GetActiveCommitmentsByAssignmentIdsAsync(
            tenantId,
            activeAssignments.Select(a => a.Id),
            cancellationToken);

        // Check each resource for overlap in the new window (excluding their own assignment's current commitment)
        foreach (var assignment in activeAssignments)
        {
            await commitmentRepository.EnsureScheduleGuardAsync(tenantId, assignment.ResourceId, cancellationToken);

            var overlaps = await commitmentRepository.GetActiveCommitmentsForResourceAsync(
                tenantId,
                assignment.ResourceId,
                request.NewStartUtc,
                request.NewEndUtc,
                cancellationToken);

            var conflicting = overlaps.Where(c => c.ResourceAssignmentId != assignment.Id).ToList();
            if (conflicting.Count > 0)
            {
                var resource = await resourceRepository.GetByIdAsync(tenantId, assignment.ResourceId, asNoTracking: true, cancellationToken);
                var resName = resource?.DisplayName ?? assignment.ResourceId.ToString();
                throw new ConflictException($"Cannot reschedule: Resource '{resName}' has an overlapping commitment in the new window ({conflicting[0].StartUtc:g} - {conflicting[0].EndUtc:g}).");
            }
        }

        // Supersede previous commitments and create new ones for the new window
        foreach (var assignment in activeAssignments)
        {
            var oldCommitment = existingCommitments.FirstOrDefault(c => c.ResourceAssignmentId == assignment.Id);
            oldCommitment?.Supersede($"Rescheduled to {request.NewStartUtc:s} - {request.NewEndUtc:s}");

            var newCommitment = new ResourceCommitment(
                tenantId: tenantId,
                resourceId: assignment.ResourceId,
                resourceAssignmentId: assignment.Id,
                startUtc: request.NewStartUtc,
                endUtc: request.NewEndUtc,
                commitmentKind: CommitmentKind.Direct);

            await commitmentRepository.AddAsync(newCommitment, cancellationToken);
        }

        // Execute domain reschedule
        booking.Reschedule(
            request.NewStartUtc,
            request.NewEndUtc,
            request.Reason,
            request.Initiator ?? "Dispatcher",
            userId);

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

using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Mappers;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Application.Scheduling.Commands.UnassignResourceFromBooking;

public sealed class UnassignResourceFromBookingCommandHandler(
    IBookingRepository bookingRepository,
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<UnassignResourceFromBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(UnassignResourceFromBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var booking = await bookingRepository.GetByIdAsync(tenantId, request.BookingId, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{request.BookingId}' was not found.");
        }

        var assignment = booking.Assignments.FirstOrDefault(a => a.Id == request.AssignmentId);
        if (assignment is null)
        {
            throw new NotFoundException($"Assignment with ID '{request.AssignmentId}' was not found on this booking.");
        }

        if (assignment.Status == AssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot unassign a completed assignment.");
        }

        // Release commitment
        var commitment = await commitmentRepository.GetActiveCommitmentByAssignmentIdAsync(tenantId, assignment.Id, cancellationToken);
        commitment?.Release($"Unassigned: {request.Reason}");

        // Cancel assignment
        assignment.Cancel(request.Reason);

        // If no more active assignments exist, revert booking dispatch status
        var hasActiveAssignments = booking.Assignments.Any(a =>
            a.Id != assignment.Id &&
            a.Status != AssignmentStatus.Cancelled &&
            a.Status != AssignmentStatus.Replaced);

        if (!hasActiveAssignments && booking.DispatchStatus != BookingDispatchStatus.Unassigned)
        {
            booking.UpdateDispatchStatus(BookingDispatchStatus.Unassigned);
        }

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

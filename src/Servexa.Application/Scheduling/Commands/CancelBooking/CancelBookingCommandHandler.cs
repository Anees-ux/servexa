using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Mappers;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;

namespace Servexa.Application.Scheduling.Commands.CancelBooking;

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId;

        var booking = await bookingRepository.GetByIdAsync(tenantId, request.BookingId, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{request.BookingId}' was not found.");
        }

        // Release all active commitments for this booking's assignments
        var commitments = await commitmentRepository.GetActiveCommitmentsByAssignmentIdsAsync(
            tenantId,
            booking.Assignments.Select(a => a.Id),
            cancellationToken);

        foreach (var commitment in commitments)
        {
            commitment.Release($"Booking cancelled: {request.Reason}");
        }

        booking.Cancel(request.Reason, userId);

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

using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Mappers;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Application.Scheduling.Commands.DispatchBooking;

public sealed class DispatchBookingCommandHandler(
    IBookingRepository bookingRepository,
    IResourceRepository resourceRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<DispatchBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(DispatchBookingCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId;

        var booking = await bookingRepository.GetByIdAsync(tenantId, request.BookingId, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{request.BookingId}' was not found.");
        }

        var hasAssigned = booking.Assignments.Any(a => a.Status == AssignmentStatus.Assigned);
        if (!hasAssigned)
        {
            throw new InvalidOperationException("Cannot dispatch a booking that has no active assigned resources.");
        }

        booking.Dispatch(userId);

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

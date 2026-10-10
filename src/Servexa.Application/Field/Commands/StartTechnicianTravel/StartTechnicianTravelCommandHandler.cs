using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Mappers;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Application.Field.Commands.StartTechnicianTravel;

public sealed class StartTechnicianTravelCommandHandler(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository,
    IExecutionSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<StartTechnicianTravelCommand, ExecutionSessionDto>
{
    public async Task<ExecutionSessionDto> Handle(StartTechnicianTravelCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        var resource = await resourceRepository.GetByUserIdAsync(tenantId, userId, cancellationToken);
        if (resource is null)
        {
            throw new ForbiddenAccessException("Authenticated user is not registered as an active field resource.");
        }

        // Find booking containing this assignment
        var bookings = await bookingRepository.GetBookingsForResourceAsync(tenantId, resource.Id, cancellationToken: cancellationToken);
        var bookingRef = bookings.FirstOrDefault(b => b.Assignments.Any(a => a.Id == request.AssignmentId));
        if (bookingRef is null)
        {
            throw new NotFoundException($"Assignment with ID '{request.AssignmentId}' was not found for this technician.");
        }

        var booking = await bookingRepository.GetByIdAsync(tenantId, bookingRef.Id, asNoTracking: false, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with ID '{bookingRef.Id}' was not found.");
        }

        var assignment = booking.Assignments.FirstOrDefault(a => a.Id == request.AssignmentId);
        if (assignment is null || assignment.ResourceId != resource.Id)
        {
            throw new ForbiddenAccessException("Technician is not authorized for this assignment.");
        }

        assignment.StartTravel();
        booking.UpdateDispatchStatus(BookingDispatchStatus.EnRoute);

        var session = await sessionRepository.GetByAssignmentIdAsync(tenantId, assignment.Id, asNoTracking: false, cancellationToken);
        if (session is null)
        {
            session = new ExecutionSession(tenantId, assignment.Id, booking.Id, booking.WorkOrderId, resource.Id, userId);
            await sessionRepository.AddAsync(session, cancellationToken);
        }

        session.StartTravel();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ExecutionSessionMapper.ToDto(session);
    }
}

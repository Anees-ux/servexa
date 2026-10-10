using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Mappers;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Application.Field.Commands.CompleteTechnicianExecution;

public sealed class CompleteTechnicianExecutionCommandHandler(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository,
    IExecutionSessionRepository sessionRepository,
    IResourceCommitmentRepository commitmentRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CompleteTechnicianExecutionCommand, ExecutionSessionDto>
{
    public async Task<ExecutionSessionDto> Handle(CompleteTechnicianExecutionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        var resource = await resourceRepository.GetByUserIdAsync(tenantId, userId, cancellationToken);
        if (resource is null)
        {
            throw new ForbiddenAccessException("Authenticated user is not registered as an active field resource.");
        }

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

        // Complete assignment
        assignment.Complete();

        // Release commitment
        var commitment = await commitmentRepository.GetActiveCommitmentByAssignmentIdAsync(tenantId, assignment.Id, cancellationToken);
        commitment?.Release("Technician execution completed");

        // End session
        var session = await sessionRepository.GetByAssignmentIdAsync(tenantId, assignment.Id, asNoTracking: false, cancellationToken);
        if (session is null)
        {
            session = new ExecutionSession(tenantId, assignment.Id, booking.Id, booking.WorkOrderId, resource.Id, userId);
            await sessionRepository.AddAsync(session, cancellationToken);
        }

        session.EndSession(request.WorkSummary);

        // Check if all assignments on this booking are completed or cancelled
        var allAssignmentsFinished = booking.Assignments.All(a =>
            a.Status == AssignmentStatus.Completed || a.Status == AssignmentStatus.Cancelled);

        if (allAssignmentsFinished)
        {
            booking.Complete(userId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ExecutionSessionMapper.ToDto(session);
    }
}

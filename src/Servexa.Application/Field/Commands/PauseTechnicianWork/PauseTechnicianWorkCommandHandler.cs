using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Mappers;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;

namespace Servexa.Application.Field.Commands.PauseTechnicianWork;

public sealed class PauseTechnicianWorkCommandHandler(
    IResourceRepository resourceRepository,
    IExecutionSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<PauseTechnicianWorkCommand, ExecutionSessionDto>
{
    public async Task<ExecutionSessionDto> Handle(PauseTechnicianWorkCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        var resource = await resourceRepository.GetByUserIdAsync(tenantId, userId, cancellationToken);
        if (resource is null)
        {
            throw new ForbiddenAccessException("Authenticated user is not registered as an active field resource.");
        }

        var session = await sessionRepository.GetByAssignmentIdAsync(tenantId, request.AssignmentId, asNoTracking: false, cancellationToken);
        if (session is null || session.ResourceId != resource.Id)
        {
            throw new NotFoundException($"Active execution session for assignment '{request.AssignmentId}' was not found.");
        }

        session.PauseWork(request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ExecutionSessionMapper.ToDto(session);
    }
}

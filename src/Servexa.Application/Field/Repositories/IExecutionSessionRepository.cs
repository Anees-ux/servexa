using Servexa.Domain.Field.Entities;

namespace Servexa.Application.Field.Repositories;

public interface IExecutionSessionRepository
{
    Task<ExecutionSession?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<ExecutionSession?> GetByAssignmentIdAsync(Guid tenantId, Guid assignmentId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionSession>> GetSessionsForUserAsync(
        Guid tenantId,
        Guid userId,
        short? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task AddAsync(ExecutionSession session, CancellationToken cancellationToken = default);
}

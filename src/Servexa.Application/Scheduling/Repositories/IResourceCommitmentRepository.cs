using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Application.Scheduling.Repositories;

public interface IResourceCommitmentRepository
{
    Task<IReadOnlyList<ResourceCommitment>> GetActiveCommitmentsForResourceAsync(
        Guid tenantId,
        Guid resourceId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    Task<ResourceCommitment?> GetActiveCommitmentByAssignmentIdAsync(Guid tenantId, Guid assignmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceCommitment>> GetActiveCommitmentsByAssignmentIdsAsync(Guid tenantId, IEnumerable<Guid> assignmentIds, CancellationToken cancellationToken = default);
    Task AddAsync(ResourceCommitment commitment, CancellationToken cancellationToken = default);
    Task AddConflictLogAsync(SchedulingConflictLog log, CancellationToken cancellationToken = default);
    Task EnsureScheduleGuardAsync(Guid tenantId, Guid resourceId, CancellationToken cancellationToken = default);
}

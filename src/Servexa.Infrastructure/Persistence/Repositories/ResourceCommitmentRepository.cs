using Microsoft.EntityFrameworkCore;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class ResourceCommitmentRepository(ServexaDbContext dbContext) : IResourceCommitmentRepository
{
    public async Task<IReadOnlyList<ResourceCommitment>> GetActiveCommitmentsForResourceAsync(
        Guid tenantId,
        Guid resourceId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        // Interval overlap condition: StartUtc < commitment.EndUtc && EndUtc > commitment.StartUtc
        return await dbContext.ResourceCommitments
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId &&
                        c.ResourceId == resourceId &&
                        c.Status == CommitmentStatus.Active &&
                        startUtc < c.EndUtc &&
                        endUtc > c.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceCommitment?> GetActiveCommitmentByAssignmentIdAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResourceCommitments
            .FirstOrDefaultAsync(c => c.TenantId == tenantId &&
                                      c.ResourceAssignmentId == assignmentId &&
                                      c.Status == CommitmentStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceCommitment>> GetActiveCommitmentsByAssignmentIdsAsync(
        Guid tenantId,
        IEnumerable<Guid> assignmentIds,
        CancellationToken cancellationToken = default)
    {
        var idList = assignmentIds.ToList();
        return await dbContext.ResourceCommitments
            .Where(c => c.TenantId == tenantId &&
                        idList.Contains(c.ResourceAssignmentId) &&
                        c.Status == CommitmentStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ResourceCommitment commitment, CancellationToken cancellationToken = default)
    {
        await dbContext.ResourceCommitments.AddAsync(commitment, cancellationToken);
    }

    public async Task AddConflictLogAsync(SchedulingConflictLog log, CancellationToken cancellationToken = default)
    {
        await dbContext.SchedulingConflictLogs.AddAsync(log, cancellationToken);
    }

    public async Task EnsureScheduleGuardAsync(Guid tenantId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.ResourceScheduleGuards
            .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.ResourceId == resourceId, cancellationToken);

        if (existing == null)
        {
            var guard = new ResourceScheduleGuard(tenantId, resourceId);
            await dbContext.ResourceScheduleGuards.AddAsync(guard, cancellationToken);
        }
        else
        {
            existing.Touch();
        }
    }
}

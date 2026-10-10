using Microsoft.EntityFrameworkCore;
using Servexa.Application.Field.Repositories;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Field.Enums;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class ExecutionSessionRepository(ServexaDbContext dbContext) : IExecutionSessionRepository
{
    public async Task<ExecutionSession?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExecutionSessions
            .Include(s => s.Intervals)
            .Where(s => s.TenantId == tenantId && s.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExecutionSession?> GetByAssignmentIdAsync(Guid tenantId, Guid assignmentId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExecutionSessions
            .Include(s => s.Intervals)
            .Where(s => s.TenantId == tenantId && s.ResourceAssignmentId == assignmentId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionSession>> GetSessionsForUserAsync(
        Guid tenantId,
        Guid userId,
        short? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ExecutionSessions
            .AsNoTracking()
            .Include(s => s.Intervals)
            .Where(s => s.TenantId == tenantId && s.UserId == userId);

        if (status.HasValue)
        {
            var st = (ExecutionSessionStatus)status.Value;
            query = query.Where(s => s.Status == st);
        }

        return await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExecutionSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.ExecutionSessions.AddAsync(session, cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(ServexaDbContext dbContext) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Bookings
            .Include(b => b.Assignments)
            .Include(b => b.Revisions)
            .Include(b => b.StatusHistory)
            .Where(b => b.TenantId == tenantId && b.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Booking?> GetByBookingNumberAsync(Guid tenantId, string bookingNumber, CancellationToken cancellationToken = default)
    {
        var norm = bookingNumber.Trim().ToUpperInvariant();
        return await dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Assignments)
            .Include(b => b.Revisions)
            .Include(b => b.StatusHistory)
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.BookingNumber == norm, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(
        Guid tenantId,
        Guid? workOrderId,
        Guid? siteId,
        short? status,
        short? dispatchStatus,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Assignments)
            .Where(b => b.TenantId == tenantId);

        if (workOrderId.HasValue)
        {
            query = query.Where(b => b.WorkOrderId == workOrderId.Value);
        }

        if (siteId.HasValue)
        {
            query = query.Where(b => b.SiteId == siteId.Value);
        }

        if (status.HasValue)
        {
            var st = (BookingStatus)status.Value;
            query = query.Where(b => b.Status == st);
        }

        if (dispatchStatus.HasValue)
        {
            var ds = (BookingDispatchStatus)dispatchStatus.Value;
            query = query.Where(b => b.DispatchStatus == ds);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(b => b.PlannedEndUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(b => b.PlannedStartUtc <= toUtc.Value);
        }

        return await query
            .OrderBy(b => b.PlannedStartUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid tenantId,
        Guid? workOrderId,
        Guid? siteId,
        short? status,
        short? dispatchStatus,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId);

        if (workOrderId.HasValue)
        {
            query = query.Where(b => b.WorkOrderId == workOrderId.Value);
        }

        if (siteId.HasValue)
        {
            query = query.Where(b => b.SiteId == siteId.Value);
        }

        if (status.HasValue)
        {
            var st = (BookingStatus)status.Value;
            query = query.Where(b => b.Status == st);
        }

        if (dispatchStatus.HasValue)
        {
            var ds = (BookingDispatchStatus)dispatchStatus.Value;
            query = query.Where(b => b.DispatchStatus == ds);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(b => b.PlannedEndUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(b => b.PlannedStartUtc <= toUtc.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsForResourceAsync(
        Guid tenantId,
        Guid resourceId,
        short? assignmentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Assignments)
            .Where(b => b.TenantId == tenantId &&
                        b.Assignments.Any(a => a.ResourceId == resourceId &&
                            (!assignmentStatus.HasValue || (short)a.Status == assignmentStatus.Value)));

        return await query
            .OrderByDescending(b => b.PlannedStartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string bookingNumber, CancellationToken cancellationToken = default)
    {
        var norm = bookingNumber.Trim().ToUpperInvariant();
        return await dbContext.Bookings
            .AnyAsync(b => b.TenantId == tenantId && b.BookingNumber == norm, cancellationToken);
    }
}

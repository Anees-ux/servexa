using Servexa.Domain.Scheduling.Entities;

namespace Servexa.Application.Scheduling.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<Booking?> GetByBookingNumberAsync(Guid tenantId, string bookingNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetBookingsAsync(
        Guid tenantId,
        Guid? workOrderId,
        Guid? siteId,
        short? status,
        short? dispatchStatus,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(
        Guid tenantId,
        Guid? workOrderId,
        Guid? siteId,
        short? status,
        short? dispatchStatus,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetBookingsForResourceAsync(
        Guid tenantId,
        Guid resourceId,
        short? assignmentStatus = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string bookingNumber, CancellationToken cancellationToken = default);
}

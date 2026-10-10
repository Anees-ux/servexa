using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Field.Dtos;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Field.Queries.GetMyAssignedJobs;

public sealed class GetMyAssignedJobsQueryHandler(
    IResourceRepository resourceRepository,
    IBookingRepository bookingRepository,
    IExecutionSessionRepository sessionRepository,
    IWorkOrderRepository workOrderRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IAssetRepository assetRepository,
    ITenantContext tenantContext) : IRequestHandler<GetMyAssignedJobsQuery, IReadOnlyList<AssignedJobDto>>
{
    public async Task<IReadOnlyList<AssignedJobDto>> Handle(GetMyAssignedJobsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = tenantContext.UserId;
        if (userId is null)
        {
            return [];
        }

        // Find the resource linked to the logged-in user
        var resource = await resourceRepository.GetByUserIdAsync(tenantId, userId.Value, cancellationToken);
        if (resource is null)
        {
            return [];
        }

        var bookings = await bookingRepository.GetBookingsForResourceAsync(
            tenantId,
            resource.Id,
            request.AssignmentStatus,
            cancellationToken);

        var result = new List<AssignedJobDto>();

        foreach (var booking in bookings)
        {
            var myAssignments = booking.Assignments
                .Where(a => a.ResourceId == resource.Id &&
                    (!request.AssignmentStatus.HasValue || (short)a.Status == request.AssignmentStatus.Value))
                .ToList();

            if (myAssignments.Count == 0) continue;

            var workOrder = await workOrderRepository.GetByIdAsync(tenantId, booking.WorkOrderId, asNoTracking: true, cancellationToken);
            var site = await siteRepository.GetByIdAsync(tenantId, booking.SiteId, cancellationToken);
            var account = workOrder != null ? await accountRepository.GetByIdAsync(tenantId, workOrder.ServiceAccountId, cancellationToken) : null;

            string? assetNumber = null;
            string? assetName = null;
            if (workOrder != null)
            {
                var primaryWoAsset = workOrder.Assets.FirstOrDefault(a => a.Role == WorkOrderAssetRole.Primary)
                    ?? workOrder.Assets.FirstOrDefault();
                if (primaryWoAsset != null)
                {
                    var asset = await assetRepository.GetByIdAsync(tenantId, primaryWoAsset.AssetId, asNoTracking: true, cancellationToken);
                    if (asset != null)
                    {
                        assetNumber = asset.AssetNumber;
                        assetName = asset.SerialNumber;
                    }
                }
            }

            foreach (var assignment in myAssignments)
            {
                var session = await sessionRepository.GetByAssignmentIdAsync(tenantId, assignment.Id, asNoTracking: true, cancellationToken);

                result.Add(new AssignedJobDto(
                    AssignmentId: assignment.Id,
                    BookingId: booking.Id,
                    BookingNumber: booking.BookingNumber,
                    WorkOrderId: booking.WorkOrderId,
                    WorkOrderNumber: workOrder?.WorkOrderNumber ?? string.Empty,
                    WorkOrderSummary: workOrder?.Summary ?? string.Empty,
                    WorkOrderDescription: workOrder?.Description,
                    Priority: workOrder?.Priority.ToString() ?? "Standard",
                    SiteId: booking.SiteId,
                    SiteName: site?.Name ?? string.Empty,
                    SiteAddress: site?.AddressLine1,
                    CustomerName: account?.DisplayName ?? account?.LegalName,
                    PrimaryAssetNumber: assetNumber,
                    PrimaryAssetName: assetName,
                    PlannedStartUtc: assignment.PlannedStartUtc,
                    PlannedEndUtc: assignment.PlannedEndUtc,
                    AssignmentRole: assignment.AssignmentRole.ToString(),
                    AssignmentStatus: assignment.Status.ToString(),
                    AssignmentStatusValue: (short)assignment.Status,
                    BookingStatus: booking.Status.ToString(),
                    DispatchStatus: booking.DispatchStatus.ToString(),
                    SessionId: session?.Id,
                    SessionStatus: session?.Status.ToString(),
                    SessionStatusValue: session != null ? (short)session.Status : null,
                    StartedAtUtc: session?.StartedAtUtc ?? assignment.DispatchedAtUtc,
                    CompletedAtUtc: session?.CompletedAtUtc ?? assignment.CompletedAtUtc,
                    WorkSummary: session?.WorkSummary));
            }
        }

        return result;
    }
}

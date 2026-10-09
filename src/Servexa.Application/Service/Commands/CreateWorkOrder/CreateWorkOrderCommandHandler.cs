using System.Text.Json;
using MediatR;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Service.Dtos;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;

namespace Servexa.Application.Service.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandHandler(
    IWorkOrderRepository workOrderRepository,
    IAccountRepository accountRepository,
    ISiteRepository siteRepository,
    IAssetRepository assetRepository,
    INumberSeriesService numberSeriesService,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    public async Task<WorkOrderDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        // Verify service account
        var serviceAccount = await accountRepository.GetByIdAsync(tenantId, request.ServiceAccountId, cancellationToken);
        if (serviceAccount is null)
        {
            throw new NotFoundException($"Service account with ID '{request.ServiceAccountId}' was not found.");
        }

        // Verify bill-to account
        var billToAccountId = request.BillToAccountId ?? request.ServiceAccountId;
        var billToAccount = billToAccountId == request.ServiceAccountId
            ? serviceAccount
            : await accountRepository.GetByIdAsync(tenantId, billToAccountId, cancellationToken);

        if (billToAccount is null)
        {
            throw new NotFoundException($"Bill-to account with ID '{billToAccountId}' was not found.");
        }

        // Verify primary site
        var primarySite = await siteRepository.GetByIdAsync(tenantId, request.PrimarySiteId, cancellationToken);
        if (primarySite is null)
        {
            throw new NotFoundException($"Primary site with ID '{request.PrimarySiteId}' was not found.");
        }

        // Verify primary asset if specified
        string? assetNumber = null;
        if (request.PrimaryAssetId.HasValue)
        {
            var asset = await assetRepository.GetByIdAsync(tenantId, request.PrimaryAssetId.Value, asNoTracking: true, cancellationToken);
            if (asset is null)
            {
                throw new NotFoundException($"Asset with ID '{request.PrimaryAssetId.Value}' was not found.");
            }
            assetNumber = asset.AssetNumber;
        }

        // Generate or check WorkOrderNumber
        string workOrderNumber;
        if (string.IsNullOrWhiteSpace(request.WorkOrderNumber))
        {
            workOrderNumber = await numberSeriesService.AllocateNextNumberAsync(tenantId, "WORK_ORDER", "WO-", cancellationToken);
        }
        else
        {
            workOrderNumber = request.WorkOrderNumber.Trim().ToUpperInvariant();
            var exists = await workOrderRepository.ExistsAsync(tenantId, workOrderNumber, cancellationToken);
            if (exists)
            {
                throw new ConflictException($"A work order with number '{workOrderNumber}' already exists for this tenant.");
            }
        }

        var billToSnapshot = JsonSerializer.Serialize(new
        {
            AccountId = billToAccount.Id,
            billToAccount.AccountNumber,
            billToAccount.LegalName,
            billToAccount.DisplayName,
            billToAccount.CurrencyCode,
            billToAccount.PaymentTermsDays
        });

        var workOrder = new WorkOrder(
            tenantId: tenantId,
            workOrderNumber: workOrderNumber,
            serviceAccountId: request.ServiceAccountId,
            billToAccountId: billToAccountId,
            primarySiteId: request.PrimarySiteId,
            workTypeCode: request.WorkTypeCode,
            priority: request.Priority,
            summary: request.Summary,
            billToSnapshotJson: billToSnapshot,
            serviceRequestId: request.ServiceRequestId,
            description: request.Description);

        if (request.PrimaryAssetId.HasValue)
        {
            workOrder.AttachAsset(request.PrimaryAssetId.Value, request.PrimarySiteId, WorkOrderAssetRole.Primary);
        }

        await workOrderRepository.AddAsync(workOrder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var assetDtos = workOrder.Assets.Select(a => new WorkOrderAssetDto(
            a.Id,
            a.AssetId,
            assetNumber,
            null,
            a.Role.ToString(),
            (short)a.Role,
            a.SiteIdAtTime,
            a.Status.ToString(),
            (short)a.Status)).ToList();

        var historyDtos = workOrder.StatusHistory.Select(h => new WorkOrderStatusHistoryDto(
            h.Id,
            h.FromStatus.ToString(),
            (short)h.FromStatus,
            h.ToStatus.ToString(),
            (short)h.ToStatus,
            h.PauseReasonCode,
            h.ChangedByUserId,
            h.ChangedAtUtc,
            h.Reason)).ToList();

        return new WorkOrderDto(
            workOrder.Id,
            workOrder.TenantId,
            workOrder.WorkOrderNumber,
            workOrder.ServiceRequestId,
            workOrder.ServiceAccountId,
            serviceAccount.DisplayName,
            workOrder.BillToAccountId,
            billToAccount.DisplayName,
            workOrder.PrimarySiteId,
            primarySite.Name,
            workOrder.WorkTypeCode,
            workOrder.Priority.ToString(),
            (short)workOrder.Priority,
            workOrder.OperationalStatus.ToString(),
            (short)workOrder.OperationalStatus,
            workOrder.Summary,
            workOrder.Description,
            workOrder.PauseReasonCode,
            workOrder.PauseNote,
            workOrder.OperationallyCompletedAtUtc,
            workOrder.CreatedAtUtc,
            workOrder.ModifiedAtUtc,
            assetDtos,
            historyDtos);
    }
}

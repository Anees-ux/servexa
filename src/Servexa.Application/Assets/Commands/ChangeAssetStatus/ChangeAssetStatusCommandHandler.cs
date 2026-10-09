using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Domain.Assets.Enums;

namespace Servexa.Application.Assets.Commands.ChangeAssetStatus;

public sealed class ChangeAssetStatusCommandHandler(
    IAssetRepository assetRepository,
    IEquipmentModelRepository equipmentModelRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<ChangeAssetStatusCommand, AssetDto>
{
    public async Task<AssetDto> Handle(ChangeAssetStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        Guid? userId = tenantContext.UserId != Guid.Empty ? tenantContext.UserId : null;

        var asset = await assetRepository.GetByIdAsync(tenantId, request.AssetId, asNoTracking: false, cancellationToken);
        if (asset is null)
        {
            throw new NotFoundException($"Asset with ID '{request.AssetId}' was not found.");
        }

        switch (request.TargetStatus)
        {
            case AssetStatus.Active:
                if (asset.Status == AssetStatus.PreInstallation)
                {
                    if (request.SiteId.HasValue && request.SiteId.Value != asset.CurrentSiteId)
                    {
                        asset.MoveToSite(request.SiteId.Value, request.Reason, userId);
                    }
                    else if (!asset.CurrentSiteId.HasValue && !request.SiteId.HasValue)
                    {
                        throw new ValidationException("Site ID is required to commission pre-installation asset.");
                    }
                    asset.Commission(reason: request.Reason, actorUserId: userId);
                }
                else if (asset.Status == AssetStatus.Degraded)
                {
                    asset.RestoreActive(request.Reason, userId);
                }
                else if (asset.Status == AssetStatus.Decommissioned)
                {
                    asset.Reactivate(request.Reason, userId);
                }
                else
                {
                    throw new ValidationException($"Cannot transition asset from {asset.Status} to Active.");
                }
                break;

            case AssetStatus.Degraded:
                asset.MarkDegraded(request.Reason, userId);
                break;

            case AssetStatus.Decommissioned:
                asset.Decommission(request.Reason, userId);
                break;

            case AssetStatus.Replaced:
                asset.Replace(request.Reason, userId);
                break;

            default:
                throw new ValidationException($"Unsupported target asset status '{request.TargetStatus}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch details for DTO
        var model = await equipmentModelRepository.GetByIdAsync(tenantId, asset.EquipmentModelId, cancellationToken);
        string? siteName = null;
        if (asset.CurrentSiteId.HasValue)
        {
            var site = await siteRepository.GetByIdAsync(tenantId, asset.CurrentSiteId.Value, cancellationToken);
            siteName = site?.Name;
        }

        string? accountName = null;
        if (asset.CurrentOwnerAccountId.HasValue)
        {
            var account = await accountRepository.GetByIdAsync(tenantId, asset.CurrentOwnerAccountId.Value, cancellationToken);
            accountName = account?.DisplayName;
        }

        var eventDtos = asset.LifecycleEvents.Select(e => new AssetLifecycleEventDto(
            e.Id,
            e.EventType.ToString(),
            (short)e.EventType,
            e.PreviousStatus?.ToString(),
            (short?)e.PreviousStatus,
            e.NewStatus?.ToString(),
            (short?)e.NewStatus,
            e.FromSiteId,
            e.ToSiteId,
            e.FromOwnerAccountId,
            e.ToOwnerAccountId,
            e.Reason,
            e.ActorUserId,
            e.OccurredAtUtc,
            e.RecordedAtUtc)).OrderByDescending(e => e.OccurredAtUtc).ToList();

        return new AssetDto(
            asset.Id,
            asset.TenantId,
            asset.AssetNumber,
            asset.EquipmentModelId,
            model?.DisplayName,
            model?.ManufacturerName,
            model?.ModelCode,
            asset.SerialNumber,
            asset.CurrentSiteId,
            siteName,
            asset.CurrentOwnerAccountId,
            accountName,
            asset.Status.ToString(),
            (short)asset.Status,
            asset.InstalledAtUtc,
            asset.DecommissionedAtUtc,
            asset.CreatedAtUtc,
            asset.ModifiedAtUtc,
            eventDtos);
    }
}

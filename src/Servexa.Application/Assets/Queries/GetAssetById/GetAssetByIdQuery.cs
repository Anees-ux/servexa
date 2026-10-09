using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;

namespace Servexa.Application.Assets.Queries.GetAssetById;

public sealed record GetAssetByIdQuery(Guid Id) : IRequest<AssetDto>;

public sealed class GetAssetByIdQueryHandler(
    IAssetRepository assetRepository,
    IEquipmentModelRepository equipmentModelRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    ITenantContext tenantContext) : IRequestHandler<GetAssetByIdQuery, AssetDto>
{
    public async Task<AssetDto> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var asset = await assetRepository.GetByIdAsync(tenantId, request.Id, asNoTracking: true, cancellationToken);
        if (asset is null)
        {
            throw new NotFoundException($"Asset with ID '{request.Id}' was not found.");
        }

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

using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Customers.Repositories;

namespace Servexa.Application.Assets.Queries.GetAssets;

public sealed record GetAssetsQuery(
    Guid? SiteId = null,
    Guid? OwnerAccountId = null,
    Guid? EquipmentModelId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<AssetDto>>;

public sealed class GetAssetsQueryHandler(
    IAssetRepository assetRepository,
    IEquipmentModelRepository equipmentModelRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    ITenantContext tenantContext) : IRequestHandler<GetAssetsQuery, PagedResult<AssetDto>>
{
    public async Task<PagedResult<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var items = await assetRepository.GetAssetsAsync(
            tenantId,
            request.SiteId,
            request.OwnerAccountId,
            request.EquipmentModelId,
            request.Search,
            skip,
            pageSize,
            cancellationToken);

        var totalCount = await assetRepository.GetCountAsync(
            tenantId,
            request.SiteId,
            request.OwnerAccountId,
            request.EquipmentModelId,
            request.Search,
            cancellationToken);

        // Pre-fetch related entities for mapping
        var modelIds = items.Select(a => a.EquipmentModelId).Distinct().ToList();
        var models = new Dictionary<Guid, (string DisplayName, string Manufacturer, string ModelCode)>();
        foreach (var mid in modelIds)
        {
            var m = await equipmentModelRepository.GetByIdAsync(tenantId, mid, cancellationToken);
            if (m is not null)
            {
                models[mid] = (m.DisplayName, m.ManufacturerName, m.ModelCode);
            }
        }

        var siteIds = items.Where(a => a.CurrentSiteId.HasValue).Select(a => a.CurrentSiteId!.Value).Distinct().ToList();
        var sites = new Dictionary<Guid, string>();
        foreach (var sid in siteIds)
        {
            var s = await siteRepository.GetByIdAsync(tenantId, sid, cancellationToken);
            if (s is not null)
            {
                sites[sid] = s.Name;
            }
        }

        var accountIds = items.Where(a => a.CurrentOwnerAccountId.HasValue).Select(a => a.CurrentOwnerAccountId!.Value).Distinct().ToList();
        var accounts = new Dictionary<Guid, string>();
        foreach (var aid in accountIds)
        {
            var a = await accountRepository.GetByIdAsync(tenantId, aid, cancellationToken);
            if (a is not null)
            {
                accounts[aid] = a.DisplayName;
            }
        }

        var dtos = items.Select(a =>
        {
            models.TryGetValue(a.EquipmentModelId, out var modelInfo);
            string? siteName = a.CurrentSiteId.HasValue && sites.TryGetValue(a.CurrentSiteId.Value, out var sn) ? sn : null;
            string? accountName = a.CurrentOwnerAccountId.HasValue && accounts.TryGetValue(a.CurrentOwnerAccountId.Value, out var an) ? an : null;

            var eventDtos = a.LifecycleEvents.Select(e => new AssetLifecycleEventDto(
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
                a.Id,
                a.TenantId,
                a.AssetNumber,
                a.EquipmentModelId,
                modelInfo.DisplayName,
                modelInfo.Manufacturer,
                modelInfo.ModelCode,
                a.SerialNumber,
                a.CurrentSiteId,
                siteName,
                a.CurrentOwnerAccountId,
                accountName,
                a.Status.ToString(),
                (short)a.Status,
                a.InstalledAtUtc,
                a.DecommissionedAtUtc,
                a.CreatedAtUtc,
                a.ModifiedAtUtc,
                eventDtos);
        }).ToList();

        return new PagedResult<AssetDto>(dtos, totalCount, pageNumber, pageSize);
    }
}

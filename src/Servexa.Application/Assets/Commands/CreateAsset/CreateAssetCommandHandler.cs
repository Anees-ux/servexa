using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Domain.Assets.Entities;
using Servexa.Domain.Assets.Enums;

namespace Servexa.Application.Assets.Commands.CreateAsset;

public sealed class CreateAssetCommandHandler(
    IAssetRepository assetRepository,
    IEquipmentModelRepository equipmentModelRepository,
    ISiteRepository siteRepository,
    IAccountRepository accountRepository,
    INumberSeriesService numberSeriesService,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateAssetCommand, AssetDto>
{
    public async Task<AssetDto> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        // Verify equipment model exists in tenant
        var model = await equipmentModelRepository.GetByIdAsync(tenantId, request.EquipmentModelId, cancellationToken);
        if (model is null)
        {
            throw new NotFoundException($"Equipment model with ID '{request.EquipmentModelId}' was not found.");
        }

        if (model.TrackingPolicy == EquipmentTrackingPolicy.Serialized && string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            throw new ValidationException("Serial number is required for serialized equipment models.");
        }

        // Verify site if provided
        string? siteName = null;
        if (request.CurrentSiteId.HasValue)
        {
            var site = await siteRepository.GetByIdAsync(tenantId, request.CurrentSiteId.Value, cancellationToken);
            if (site is null)
            {
                throw new NotFoundException($"Site with ID '{request.CurrentSiteId.Value}' was not found.");
            }
            siteName = site.Name;
        }

        // Verify owner account if provided
        string? accountName = null;
        if (request.CurrentOwnerAccountId.HasValue)
        {
            var account = await accountRepository.GetByIdAsync(tenantId, request.CurrentOwnerAccountId.Value, cancellationToken);
            if (account is null)
            {
                throw new NotFoundException($"Customer account with ID '{request.CurrentOwnerAccountId.Value}' was not found.");
            }
            accountName = account.DisplayName;
        }

        // Generate or validate asset number
        string assetNumber;
        if (string.IsNullOrWhiteSpace(request.AssetNumber))
        {
            assetNumber = await numberSeriesService.AllocateNextNumberAsync(tenantId, "ASSET", "AST-", cancellationToken);
        }
        else
        {
            assetNumber = request.AssetNumber.Trim().ToUpperInvariant();
            var exists = await assetRepository.ExistsAsync(tenantId, assetNumber, cancellationToken);
            if (exists)
            {
                throw new ConflictException($"An asset with number '{assetNumber}' already exists for this tenant.");
            }
        }

        var asset = new Asset(
            tenantId: tenantId,
            assetNumber: assetNumber,
            equipmentModelId: request.EquipmentModelId,
            serialNumber: request.SerialNumber,
            currentSiteId: request.CurrentSiteId,
            currentOwnerAccountId: request.CurrentOwnerAccountId,
            status: request.Status,
            actorUserId: tenantContext.UserId != Guid.Empty ? tenantContext.UserId : null);

        await assetRepository.AddAsync(asset, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
            e.RecordedAtUtc)).ToList();

        return new AssetDto(
            asset.Id,
            asset.TenantId,
            asset.AssetNumber,
            asset.EquipmentModelId,
            model.DisplayName,
            model.ManufacturerName,
            model.ModelCode,
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

using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Domain.Assets.Enums;

namespace Servexa.Application.Assets.Commands.CreateAsset;

public sealed record CreateAssetCommand(
    string? AssetNumber,
    Guid EquipmentModelId,
    string? SerialNumber = null,
    Guid? CurrentSiteId = null,
    Guid? CurrentOwnerAccountId = null,
    AssetStatus Status = AssetStatus.Active) : IRequest<AssetDto>;

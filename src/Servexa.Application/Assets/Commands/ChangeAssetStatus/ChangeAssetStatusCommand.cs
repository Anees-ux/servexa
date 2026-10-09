using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Domain.Assets.Enums;

namespace Servexa.Application.Assets.Commands.ChangeAssetStatus;

public sealed record ChangeAssetStatusCommand(
    Guid AssetId,
    AssetStatus TargetStatus,
    string? Reason = null,
    Guid? SiteId = null) : IRequest<AssetDto>;

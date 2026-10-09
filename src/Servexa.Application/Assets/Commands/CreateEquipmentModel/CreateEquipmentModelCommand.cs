using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Domain.Assets.Enums;

namespace Servexa.Application.Assets.Commands.CreateEquipmentModel;

public sealed record CreateEquipmentModelCommand(
    string ManufacturerName,
    string ModelCode,
    string DisplayName,
    string CategoryCode,
    EquipmentTrackingPolicy TrackingPolicy) : IRequest<EquipmentModelDto>;

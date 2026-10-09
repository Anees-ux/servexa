using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Domain.Assets.Entities;

namespace Servexa.Application.Assets.Commands.CreateEquipmentModel;

public sealed class CreateEquipmentModelCommandHandler(
    IEquipmentModelRepository equipmentModelRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext) : IRequestHandler<CreateEquipmentModelCommand, EquipmentModelDto>
{
    public async Task<EquipmentModelDto> Handle(CreateEquipmentModelCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var exists = await equipmentModelRepository.ExistsAsync(tenantId, request.ManufacturerName, request.ModelCode, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"An equipment model with manufacturer '{request.ManufacturerName}' and code '{request.ModelCode}' already exists for this tenant.");
        }

        var model = new EquipmentModel(
            tenantId: tenantId,
            manufacturerName: request.ManufacturerName,
            modelCode: request.ModelCode,
            displayName: request.DisplayName,
            categoryCode: request.CategoryCode,
            trackingPolicy: request.TrackingPolicy);

        await equipmentModelRepository.AddAsync(model, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new EquipmentModelDto(
            model.Id,
            model.TenantId,
            model.ManufacturerName,
            model.ModelCode,
            model.DisplayName,
            model.CategoryCode,
            model.TrackingPolicy.ToString(),
            (short)model.TrackingPolicy,
            model.Status.ToString(),
            (short)model.Status,
            model.CreatedAtUtc,
            model.ModifiedAtUtc);
    }
}

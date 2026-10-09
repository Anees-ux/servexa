using MediatR;
using Servexa.Application.Assets.Dtos;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;

namespace Servexa.Application.Assets.Queries.GetEquipmentModels;

public sealed record GetEquipmentModelsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<EquipmentModelDto>>;

public sealed class GetEquipmentModelsQueryHandler(
    IEquipmentModelRepository equipmentModelRepository,
    ITenantContext tenantContext) : IRequestHandler<GetEquipmentModelsQuery, PagedResult<EquipmentModelDto>>
{
    public async Task<PagedResult<EquipmentModelDto>> Handle(GetEquipmentModelsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var items = await equipmentModelRepository.GetEquipmentModelsAsync(tenantId, request.Search, skip, pageSize, cancellationToken);
        var totalCount = await equipmentModelRepository.GetCountAsync(tenantId, request.Search, cancellationToken);

        var dtos = items.Select(m => new EquipmentModelDto(
            m.Id,
            m.TenantId,
            m.ManufacturerName,
            m.ModelCode,
            m.DisplayName,
            m.CategoryCode,
            m.TrackingPolicy.ToString(),
            (short)m.TrackingPolicy,
            m.Status.ToString(),
            (short)m.Status,
            m.CreatedAtUtc,
            m.ModifiedAtUtc)).ToList();

        return new PagedResult<EquipmentModelDto>(dtos, totalCount, pageNumber, pageSize);
    }
}

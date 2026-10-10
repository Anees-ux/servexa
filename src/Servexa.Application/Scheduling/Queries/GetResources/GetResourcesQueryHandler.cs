using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Common.Models;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Repositories;

namespace Servexa.Application.Scheduling.Queries.GetResources;

public sealed class GetResourcesQueryHandler(
    IResourceRepository resourceRepository,
    ITenantContext tenantContext) : IRequestHandler<GetResourcesQuery, PagedResult<ResourceDto>>
{
    public async Task<PagedResult<ResourceDto>> Handle(GetResourcesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var skip = (request.PageNumber - 1) * request.PageSize;

        var items = await resourceRepository.GetResourcesAsync(
            tenantId,
            request.Status,
            request.ResourceType,
            request.BranchId,
            skip,
            request.PageSize,
            cancellationToken);

        var totalCount = await resourceRepository.GetCountAsync(
            tenantId,
            request.Status,
            request.ResourceType,
            request.BranchId,
            cancellationToken);

        var dtos = items.Select(r => new ResourceDto(
            r.Id,
            r.ResourceCode,
            r.DisplayName,
            r.ResourceType.ToString(),
            (short)r.ResourceType,
            r.Status.ToString(),
            (short)r.Status,
            r.UserId,
            r.HomeBranchId,
            r.ExclusiveCapacity)).ToList();

        return new PagedResult<ResourceDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}

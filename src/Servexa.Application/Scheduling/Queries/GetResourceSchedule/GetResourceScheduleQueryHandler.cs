using MediatR;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Repositories;

namespace Servexa.Application.Scheduling.Queries.GetResourceSchedule;

public sealed class GetResourceScheduleQueryHandler(
    IResourceRepository resourceRepository,
    IResourceCommitmentRepository commitmentRepository,
    ITenantContext tenantContext) : IRequestHandler<GetResourceScheduleQuery, ResourceScheduleDto>
{
    public async Task<ResourceScheduleDto> Handle(GetResourceScheduleQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var resource = await resourceRepository.GetByIdAsync(tenantId, request.ResourceId, asNoTracking: true, cancellationToken);
        if (resource is null)
        {
            throw new NotFoundException($"Resource with ID '{request.ResourceId}' was not found.");
        }

        var commitments = await commitmentRepository.GetActiveCommitmentsForResourceAsync(
            tenantId,
            request.ResourceId,
            request.StartUtc,
            request.EndUtc,
            cancellationToken);

        var commitmentDtos = commitments.Select(c => new ResourceCommitmentDto(
            c.Id,
            c.ResourceId,
            c.StartUtc,
            c.EndUtc,
            c.CommitmentKind.ToString(),
            c.Status.ToString(),
            null,
            null)).ToList();

        return new ResourceScheduleDto(
            resource.Id,
            resource.ResourceCode,
            resource.DisplayName,
            commitmentDtos);
    }
}

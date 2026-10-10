using MediatR;
using Servexa.Application.Common.Models;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Queries.GetResources;

public sealed record GetResourcesQuery(
    short? Status = null,
    short? ResourceType = null,
    Guid? BranchId = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<ResourceDto>>;

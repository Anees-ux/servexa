using MediatR;
using Servexa.Application.Scheduling.Dtos;

namespace Servexa.Application.Scheduling.Queries.GetResourceSchedule;

public sealed record GetResourceScheduleQuery(
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc) : IRequest<ResourceScheduleDto>;

using MediatR;
using Servexa.Application.Field.Dtos;

namespace Servexa.Application.Field.Queries.GetMyAssignedJobs;

public sealed record GetMyAssignedJobsQuery(short? AssignmentStatus = null) : IRequest<IReadOnlyList<AssignedJobDto>>;

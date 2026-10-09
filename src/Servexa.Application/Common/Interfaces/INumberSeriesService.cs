namespace Servexa.Application.Common.Interfaces;

/// <summary>
/// Service providing tenant-scoped allocation and formatting of human-facing business numbers.
/// </summary>
public interface INumberSeriesService
{
    Task<string> AllocateNextNumberAsync(
        Guid tenantId,
        string seriesKey,
        string defaultPrefix,
        CancellationToken cancellationToken = default);
}

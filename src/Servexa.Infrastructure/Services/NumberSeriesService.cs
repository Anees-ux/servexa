using Microsoft.EntityFrameworkCore;
using Servexa.Application.Common.Interfaces;
using Servexa.Domain.Platform.Entities;
using Servexa.Infrastructure.Persistence;

namespace Servexa.Infrastructure.Services;

public sealed class NumberSeriesService(ServexaDbContext dbContext) : INumberSeriesService
{
    public async Task<string> AllocateNextNumberAsync(
        Guid tenantId,
        string seriesKey,
        string defaultPrefix,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = seriesKey.Trim().ToUpperInvariant();
        var series = await dbContext.NumberSeries
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SeriesKey == normalizedKey && s.ScopeKey == "DEFAULT", cancellationToken);

        if (series == null)
        {
            series = new NumberSeries(
                tenantId: tenantId,
                seriesKey: normalizedKey,
                scopeKey: "DEFAULT",
                prefixPattern: defaultPrefix,
                startValue: 1001);

            await dbContext.NumberSeries.AddAsync(series, cancellationToken);
        }

        var nextVal = series.AllocateNextValue();
        await dbContext.SaveChangesAsync(cancellationToken);

        var prefix = series.PrefixPattern ?? defaultPrefix;
        return $"{prefix}{nextVal:D6}";
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Servexa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Infrastructure registrations (persistence, external adapters, background jobs)
        // will be added incrementally as bounded slices are materialized.
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Servexa.Infrastructure.Persistence;

namespace Servexa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ServexaDatabase");

        services.AddDbContext<ServexaDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(ServexaDbContext).Assembly.FullName);
                });
            }
            else
            {
                options.UseSqlServer(sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(ServexaDbContext).Assembly.FullName);
                });
            }
        });

        return services;
    }
}

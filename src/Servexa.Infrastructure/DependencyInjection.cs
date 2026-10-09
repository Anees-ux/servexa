using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Infrastructure.Identity;
using Servexa.Infrastructure.Persistence;
using Servexa.Infrastructure.Persistence.Repositories;

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

        // Security & Tokens
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITenantResolutionService, TenantResolutionService>();
        services.AddScoped<IDevDataSeeder, DevDataSeeder>();

        // Aggregate-specific Repositories & Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ISiteRepository, SiteRepository>();

        return services;
    }
}

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

        // Services
        services.AddScoped<INumberSeriesService, Services.NumberSeriesService>();

        // Aggregate-specific Repositories & Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<Application.Assets.Repositories.IEquipmentModelRepository, EquipmentModelRepository>();
        services.AddScoped<Application.Assets.Repositories.IAssetRepository, AssetRepository>();
        services.AddScoped<Application.Service.Repositories.IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<Application.Scheduling.Repositories.IBookingRepository, BookingRepository>();
        services.AddScoped<Application.Scheduling.Repositories.IResourceRepository, ResourceRepository>();
        services.AddScoped<Application.Scheduling.Repositories.IResourceCommitmentRepository, ResourceCommitmentRepository>();
        services.AddScoped<Application.Field.Repositories.IExecutionSessionRepository, ExecutionSessionRepository>();
        services.AddScoped<Application.Field.Repositories.IWorkTaskRepository, WorkTaskRepository>();

        return services;
    }
}

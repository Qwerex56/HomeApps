using HomeApps.BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//#if (sample)
using ServiceName.Application.Abstractions;
using ServiceName.Infrastructure.Echoes;
//#endif
using ServiceName.Infrastructure.Persistence;

namespace ServiceName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHomeAppsInfrastructure<ServiceNameDbContext>(
            configuration,
            serviceName: "servicelower",
            consumersAssembly: typeof(DependencyInjection).Assembly);

        // Repositories: one per aggregate.
//#if (sample)
        services.AddScoped<IEchoRepository, EchoRepository>();
//#endif

        return services;
    }
}

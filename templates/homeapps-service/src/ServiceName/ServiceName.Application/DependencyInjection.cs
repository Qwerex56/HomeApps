using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceName.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) =>
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly, configuration["MediatR:LicenseKey"]);
}

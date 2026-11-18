using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Devices.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the pipeline from the kit. Ports come from Infrastructure and the Api (the links).</summary>
    public static IServiceCollection AddDevicesApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<Common.DescriptorIntake>();
        services.AddScoped<Common.AdminNotices>();
        return services;
    }
}

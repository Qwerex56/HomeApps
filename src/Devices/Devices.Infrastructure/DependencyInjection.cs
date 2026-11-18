using Devices.Application.Abstractions;
using Devices.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Postgres with the outbox and the <c>UserErased</c> consumer. The Api adds the links (<see cref="IDeviceLinks"/>).</summary>
    public static IServiceCollection AddDevicesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<DevicesDbContext>(configuration, "devices", typeof(DependencyInjection).Assembly);
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        return services;
    }
}

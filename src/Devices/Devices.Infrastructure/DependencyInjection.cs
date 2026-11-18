using Devices.Infrastructure.Auth;
using Devices.Infrastructure.Households;
using Devices.Application.Abstractions;
using Devices.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Postgres with the outbox, the <c>UserErased</c> consumer and Households (<c>Devices:HouseholdsUrl</c>), Auth for pairing (<c>Devices:AuthUrl</c>). The Api adds the links (<see cref="IDeviceLinks"/>).</summary>
    public static IServiceCollection AddDevicesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<DevicesDbContext>(configuration, "devices", typeof(DependencyInjection).Assembly);
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddHttpContextAccessor();
        var households = configuration["Devices:HouseholdsUrl"] is { Length: > 0 } url ? url : "http://households:8080";
        services.AddHttpClient<IHouseholdRoles, HouseholdsHttpRoles>(http => http.BaseAddress = new Uri(households.TrimEnd('/') + "/"));
        var auth = configuration["Devices:AuthUrl"] is { Length: > 0 } authUrl ? authUrl : "http://auth:8080";
        services.AddHttpClient<ISatelliteIdentities, AuthSatelliteIdentities>(http => http.BaseAddress = new Uri(auth.TrimEnd('/') + "/"));
        return services;
    }
}

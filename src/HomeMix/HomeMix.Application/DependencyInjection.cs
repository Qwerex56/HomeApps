using HomeApps.BuildingBlocks.Application;
using HomeMix.Application.Data;
using HomeMix.Application.Installations;
using HomeMix.Application.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMix.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the kit's pipeline, and the shared intake and access checks.</summary>
    public static IServiceCollection AddHomeMixApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PackageIntake>();
        services.AddScoped<InstallationAccess>();
        services.AddScoped<DataAccess>();
        services.AddScoped<Blocks.Blocklist>();
        return services;
    }
}

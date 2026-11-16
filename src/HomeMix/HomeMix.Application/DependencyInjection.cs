using HomeApps.BuildingBlocks.Application;
using HomeMix.Application.Data;
using HomeMix.Application.Installations;
using HomeMix.Application.Packages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMix.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the kit's pipeline, realtime after the commit, and the shared intake and access checks.</summary>
    public static IServiceCollection AddHomeMixApplication(this IServiceCollection services)
    {
        // Before the kit's pipeline, so it is the outermost behavior and sends after the commit.
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Realtime.RealtimeBehavior<,>));
        services.AddScoped<Realtime.RealtimeOutbox>();
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PackageIntake>();
        services.AddScoped<InstallationAccess>();
        services.AddScoped<DataAccess>();
        services.AddScoped<Blocks.Blocklist>();
        services.AddScoped<Store.StoreCatalog>();
        services.AddScoped<Store.StoreNumbers>();
        return services;
    }
}

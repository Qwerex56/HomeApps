using Households.Application.Common;
using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Households.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the pipeline from the kit, default <see cref="HouseholdSettings"/>. Ports come from Infrastructure.</summary>
    public static IServiceCollection AddHouseholdsApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new HouseholdSettings());
        return services;
    }
}

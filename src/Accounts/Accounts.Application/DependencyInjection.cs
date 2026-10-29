using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the pipeline from the kit. Ports come from Infrastructure.</summary>
    public static IServiceCollection AddAccountsApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}

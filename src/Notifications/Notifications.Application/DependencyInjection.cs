using HomeApps.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Application.Delivery;

namespace Notifications.Application;

public static class DependencyInjection
{
    /// <summary>Handlers, validators and the pipeline from the kit, and the dispatcher both delivery commands share. Ports come from Infrastructure.</summary>
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddHomeAppsApplication(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<NotificationDispatcher>();
        return services;
    }
}

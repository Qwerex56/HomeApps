using HomeApps.BuildingBlocks.Infrastructure;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Push;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// The adapters behind Application's ports: Postgres with the outbox, the consumers (<c>NotificationRequested</c>,
    /// <c>UserDisabled</c>, <c>UserErased</c>) and Web Push with the instance's VAPID keys. The Api calls
    /// <c>AddNotificationsApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<NotificationsDbContext>(configuration, "notifications", typeof(DependencyInjection).Assembly);
        services.AddScoped<IRecipientRepository, RecipientRepository>();

        services.AddOptions<VapidOptions>().BindConfiguration(VapidOptions.SectionName);
        services.AddSingleton<VapidKeys>();
        services.AddHttpClient<PushServiceClient>(http => http.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IPushSender, WebPushSender>();

        return services;
    }
}

using System.Text.Json.Serialization;

namespace Notifications.Api;

internal static class DependencyInjection
{
    /// <summary>What only the Notifications Api needs on top of the kit: enums as strings in JSON.</summary>
    public static IServiceCollection AddNotificationsApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }
}

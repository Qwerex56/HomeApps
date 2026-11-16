using System.Text.Json.Serialization;
using HomeMix.Api.Http;
using HomeMix.Api.Mcp;
using HomeMix.Api.Realtime;

namespace HomeMix.Api;

internal static class DependencyInjection
{
    /// <summary>What only the HomeMix Api needs on top of the kit: enums as names, the Owner policy, the notification limit, the add-ons' MCP tools and the realtime hub.</summary>
    public static IServiceCollection AddHomeMixApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // The add-ons' declared tools, per person (A6): handlers instead of tool classes.
        services.AddHomeMixMcpTools();
        services.AddAuthorizationBuilder().AddPolicy(Policies.Owner, policy => policy.RequireRole("Owner"));
        services.AddNotifyLimit();
        services.AddHomeMixRealtime();
        return services;
    }
}

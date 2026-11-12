using System.Text.Json.Serialization;
using HomeMix.Api.Http;
using ModelContextProtocol.Server;

namespace HomeMix.Api;

internal static class DependencyInjection
{
    /// <summary>What only the HomeMix Api needs on top of the kit: enums as names, the Owner policy and the notification limit.</summary>
    public static IServiceCollection AddHomeMixApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // No tool types yet (add-ons' tools come in stage A6), but JARVIS lists this source from J2 on: an empty tool
        // collection still answers tools/list, with nothing.
        services.PostConfigure<McpServerOptions>(mcp => mcp.ToolCollection ??= []);
        services.AddAuthorizationBuilder().AddPolicy(Policies.Owner, policy => policy.RequireRole("Owner"));
        services.AddNotifyLimit();
        return services;
    }
}

using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace HomeMix.Api;

internal static class DependencyInjection
{
    /// <summary>What only the HomeMix Api needs on top of the kit.</summary>
    public static IServiceCollection AddHomeMixApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // No tool types yet (add-ons' tools come in stage A6), but JARVIS lists this source from J2 on: an empty tool
        // collection still answers tools/list, with nothing.
        services.PostConfigure<McpServerOptions>(mcp => mcp.ToolCollection ??= []);
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HomeApps.BuildingBlocks.Mcp;

public static class McpToolNames
{
    /// <summary>The names of the tools this service registered, e.g. to check them against a plugin's manifest.</summary>
    public static IReadOnlyList<string> Registered(IServiceProvider services) =>
        [.. services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).Order(StringComparer.Ordinal)];
}

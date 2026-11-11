using System.Reflection;
using HomeApps.BuildingBlocks.Api.Assistant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace HomeApps.BuildingBlocks.Mcp;

/// <summary>
/// An MCP server (Streamable HTTP, stateless) for JARVIS and other MCP clients (docs/mcp.md). Tools are classes
/// marked <c>[McpServerToolType]</c> next to the endpoints; a tool method sends the same MediatR request as the
/// endpoint and returns <see cref="ToolResult"/>, so permissions, validation and errors are exactly those of HTTP.
/// The caller's token is required like on any endpoint.
/// </summary>
public static class McpHostingExtensions
{
    /// <summary>Registers the server and every tool type in <paramref name="toolsAssembly"/>.</summary>
    /// <param name="serverName">Shown to MCP clients, e.g. <c>homeapps-households</c>.</param>
    public static WebApplicationBuilder AddHomeAppsMcp(
        this WebApplicationBuilder builder, string serverName, Assembly toolsAssembly, string? instructions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentNullException.ThrowIfNull(toolsAssembly);

        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = serverName,
                    Version = toolsAssembly.GetName().Version?.ToString(3) ?? "0.0.0",
                };
                options.ServerInstructions = instructions;
            })
            .WithHttpTransport(options => options.Stateless = true)
            .WithToolsFromAssembly(toolsAssembly);

        return builder;
    }

    /// <summary>
    /// Maps the server at <paramref name="pattern"/>. It is reachable only inside the Docker network (Traefik does
    /// not route it); MCP clients outside come through the JARVIS gateway. An assistant acting for a person may call
    /// it even in a service that refuses its changes: the tools of such a service only read.
    /// </summary>
    public static IEndpointConventionBuilder MapHomeAppsMcp(this IEndpointRouteBuilder app, string pattern = McpPaths.Default)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.MapMcp(pattern).AllowAssistant().WithTags("MCP");
    }
}

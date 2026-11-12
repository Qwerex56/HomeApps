using System.Text.Json;
using HomeApps.BuildingBlocks.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HomeMix.Api.Mcp;

/// <summary>
/// HomeMix's MCP server serves no tool classes: tools/list and tools/call are handlers over <see cref="ToolCatalog"/>,
/// because each person has their own add-ons. The kit's server (stateless HTTP at <c>/mcp</c>, the caller's token)
/// stays as it is; these handlers replace its empty tool list.
/// </summary>
internal static class HomeMixMcp
{
    public static IServiceCollection AddHomeMixMcpTools(this IServiceCollection services)
    {
        services.AddScoped<ToolCatalog>();
        services.AddScoped<ToolRunner>();
        services.AddScoped<ScriptRunner>();
        services.AddScoped<ScriptBridge>();

        // ctx.fetch: only to the add-on's network origins, checked per call; never follows a redirect elsewhere.
        services.AddHttpClient(ScriptBridge.HttpClientName, http => http.Timeout = ScriptLimits.FetchTime)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.Configure<McpServerOptions>(mcp =>
        {
            mcp.Handlers.ListToolsHandler = ListAsync;
            mcp.Handlers.CallToolHandler = CallAsync;
        });
        return services;
    }

    private static async ValueTask<ListToolsResult> ListAsync(RequestContext<ListToolsRequestParams> context, CancellationToken cancellationToken)
    {
        var tools = await Services(context).GetRequiredService<ToolCatalog>().ListAsync(cancellationToken);
        return new ListToolsResult
        {
            Tools = [.. tools.Select(t => new Tool
            {
                Name = t.Name,
                Title = t.Title,
                Description = t.Description,
                InputSchema = t.InputSchema,
                Annotations = new ToolAnnotations
                {
                    Title = t.Title,
                    ReadOnlyHint = t.ReadOnly,
                    DestructiveHint = t.Destructive,
                    IdempotentHint = t.ReadOnly,
                    OpenWorldHint = false,
                },
            })],
        };
    }

    private static async ValueTask<CallToolResult> CallAsync(RequestContext<CallToolRequestParams> context, CancellationToken cancellationToken)
    {
        var request = context.Params!;
        var arguments = request.Arguments?.ToDictionary(a => a.Key, a => a.Value);
        var result = await Services(context).GetRequiredService<ToolRunner>().CallAsync(request.Name, arguments, cancellationToken);
        return result.IsSuccess ? ToolResult.Text(result.Value) : ToolResult.Error(result.Errors);
    }

    private static IServiceProvider Services<T>(RequestContext<T> context) =>
        context.Services ?? throw new InvalidOperationException("MCP handlers run inside a request with its services.");
}

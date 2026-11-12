using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Api.Tests;

/// <summary>An MCP server in memory standing in for a service, a plugin (with its manifest) or HomeMix.</summary>
internal sealed class FakeSource : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FakeSource(WebApplication app) => _app = app;

    public HttpMessageHandler Handler => _app.GetTestServer().CreateHandler();

    public static async Task<FakeSource> StartAsync(string mcpPath, Action<IMcpServerBuilder> tools, string? pluginId = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHttpContextAccessor();
        tools(builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true));
        var app = builder.Build();
        app.MapMcp(mcpPath);
        if (pluginId is not null)
        {
            app.MapGet($"/plugins/{pluginId}/manifest", () => Results.Json(new { id = pluginId, endpoints = new { mcp = mcpPath } }));
        }

        await app.StartAsync();
        return new FakeSource(app);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

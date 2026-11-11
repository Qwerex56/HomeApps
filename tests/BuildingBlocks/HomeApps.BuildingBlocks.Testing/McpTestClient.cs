using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace HomeApps.BuildingBlocks.Testing;

/// <summary>
/// An MCP client over a test server's <see cref="HttpClient"/> (with its token), to call a service's or plugin's
/// tools the way JARVIS does.
/// </summary>
public static class McpTestClient
{
    /// <param name="endpoint">The MCP path relative to the client's base address, e.g. <c>mcp</c> or <c>plugins/hello/mcp</c>.</param>
    public static Task<McpClient> ConnectAsync(HttpClient http, string endpoint = "mcp", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(http);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, endpoint),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http,
            ownsHttpClient: false);
        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    /// <summary>The text a tool answered with, and whether it reported an error.</summary>
    public static async Task<(string Text, bool IsError)> CallTextAsync(
        this McpClient client, string tool, IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        var text = string.Join(Environment.NewLine, result.Content.OfType<TextContentBlock>().Select(t => t.Text));
        return (text, result.IsError == true);
    }
}

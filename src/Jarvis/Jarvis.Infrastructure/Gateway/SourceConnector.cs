using Jarvis.Application.Gateway;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Jarvis.Infrastructure.Gateway;

/// <summary>An MCP client to one source with one person's delegated token. Never shared between people.</summary>
public sealed class SourceConnector(IHttpClientFactory httpClients, ILoggerFactory loggers)
{
    public const string HttpClientName = "jarvis-sources";

    public async Task<McpClient> ConnectAsync(ToolSource source, string token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = source.Endpoint,
                Name = source.Name,
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            },
            httpClients.CreateClient(HttpClientName),
            loggers,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }
}

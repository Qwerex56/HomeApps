using System.Text.Json;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Jarvis.Infrastructure.Tools;

/// <summary>A confirmed action, called on the source that owns the tool with a fresh delegated token of the person.</summary>
public sealed class GatewayToolExecutor(IToolSources sources, SourceConnector connector, IDelegation delegation) : IToolExecutor
{
    public async Task<ToolOutcome> CallAsync(Guid userId, string tool, string? arguments, CancellationToken cancellationToken)
    {
        foreach (var source in await sources.ListAsync(cancellationToken))
        {
            if (source.Own(tool) is not { } own)
            {
                continue;
            }

            var token = await delegation.TokenForAsync(userId, "app", "confirmation", cancellationToken);
            try
            {
                await using var client = await connector.ConnectAsync(source, token, cancellationToken);
                var result = await client.CallToolAsync(new CallToolRequestParams
                {
                    Name = own,
                    Arguments = string.IsNullOrWhiteSpace(arguments) ? null : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments),
                }, cancellationToken);
                return new ToolOutcome(TextOf(result), result.IsError == true);
            }
            catch (Exception exception) when (exception is McpException or HttpRequestException)
            {
                return new ToolOutcome($"{source.Name} did not answer; try again in a moment.", true);
            }
        }

        return new ToolOutcome($"There is no tool named {tool}.", true);
    }

    public static string TextOf(CallToolResult result) =>
        string.Join(Environment.NewLine, result.Content.OfType<TextContentBlock>().Select(t => t.Text));
}

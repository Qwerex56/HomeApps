using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// Talks to an OpenAI-compatible chat completions API (streamed) and runs the tool-calling loop up to
/// <see cref="OpenAiChatOptions.MaxTurns"/>: the tools are the gateway's, listed and called over MCP with the person's
/// delegated token and the run's id in <c>X-Jarvis-Run</c> (no reference to the API project).
/// </summary>
internal sealed partial class OpenAiChatBrain(
    IHttpClientFactory httpClients, ILoggerFactory loggers, ILlmConnection connection, ILogger<OpenAiChatBrain> logger) : IAssistantBrain
{
    public const string HttpClientName = "jarvis-model";
    public const string Unavailable = "JARVIS jest chwilowo niedostępny";

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        var settings = await connection.CurrentAsync(cancellationToken);
        return !string.IsNullOrWhiteSpace(settings.BaseUrl) && !string.IsNullOrWhiteSpace(settings.Model);
    }

    public async IAsyncEnumerable<BrainEvent> RunAsync(BrainRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var settings = await connection.CurrentAsync(cancellationToken);
        McpClient? gateway = null;
        IList<McpClientTool> tools = [];
        var unreachable = false;
        try
        {
            using var connecting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connecting.CancelAfter(settings.Timeout);
            gateway = await ConnectAsync(settings, request, connecting.Token);
            tools = await gateway.ListToolsAsync(cancellationToken: connecting.Token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogFailed(exception);
            unreachable = true;
        }

        if (unreachable)
        {
            if (gateway is not null)
            {
                await gateway.DisposeAsync();
            }

            yield return new BrainFinished(Unavailable, null, null);
            yield break;
        }

        await using var client = gateway!;
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = request.SystemPrompt },
            new JsonObject { ["role"] = "user", ["content"] = request.Prompt },
        };
        var toolList = ToolList(tools);
        var answer = new StringBuilder();
        for (var turn = 0; turn < Math.Max(1, settings.MaxTurns); turn++)
        {
            var calls = new SortedDictionary<int, ToolCallBuilder>();
            var text = new StringBuilder();
            var failed = false;
            var stream = StreamAsync(settings, messages, toolList, cancellationToken).GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    try
                    {
                        if (!await stream.MoveNextAsync())
                        {
                            break;
                        }
                    }
                    catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException
                        && !cancellationToken.IsCancellationRequested)
                    {
                        LogFailed(exception);
                        failed = true;
                        break;
                    }

                    var delta = stream.Current;
                    if (!string.IsNullOrEmpty(delta.Text))
                    {
                        text.Append(delta.Text);
                        yield return new BrainText(delta.Text);
                    }

                    if (delta.CallIndex is { } index)
                    {
                        if (!calls.TryGetValue(index, out var call))
                        {
                            calls[index] = call = new ToolCallBuilder();
                        }

                        call.Add(delta);
                    }
                }
            }
            finally
            {
                await stream.DisposeAsync();
            }

            if (failed)
            {
                yield return new BrainFinished(Unavailable, settings.Model, null);
                yield break;
            }

            answer.Append(text);
            if (calls.Count == 0)
            {
                yield return new BrainFinished(answer.ToString(), settings.Model, null);
                yield break;
            }

            var assistant = new JsonObject { ["role"] = "assistant", ["content"] = text.Length == 0 ? null : text.ToString() };
            var wire = new JsonArray();
            foreach (var call in calls.Values)
            {
                wire.Add(new JsonObject
                {
                    ["id"] = call.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.Arguments.ToString() },
                });
            }

            assistant["tool_calls"] = wire;
            messages.Add(assistant);
            foreach (var call in calls.Values)
            {
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = await CallAsync(client, call, cancellationToken),
                });
            }
        }

        // The turn limit ran out with tool calls still unresolved: no half answer.
        yield return new BrainFinished(Unavailable, settings.Model, null);
    }

    private async Task<McpClient> ConnectAsync(OpenAiChatOptions settings, BrainRequest request, CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(settings.GatewayUrl),
                Name = "homeapps",
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {request.GatewayToken}",
                    [RunHeader.Name] = request.RunId.ToString(),
                },
            },
            httpClients.CreateClient(SourceConnector.HttpClientName),
            loggers,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private static JsonArray ToolList(IList<McpClientTool> tools)
    {
        var list = new JsonArray();
        foreach (var tool in tools)
        {
            list.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonNode.Parse(tool.JsonSchema.GetRawText()),
                },
            });
        }

        return list;
    }

    private static async Task<string> CallAsync(McpClient client, ToolCallBuilder call, CancellationToken cancellationToken)
    {
        try
        {
            var arguments = call.Arguments.Length == 0
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(call.Arguments.ToString()) ?? [];
            var result = await client.CallToolAsync(call.Name, arguments, cancellationToken: cancellationToken);
            var text = string.Join('\n', result.Content.OfType<TextContentBlock>().Select(b => b.Text));
            return result.IsError == true ? $"Błąd: {text}" : text;
        }
        catch (Exception exception) when (exception is McpException or JsonException)
        {
            return $"Błąd: {exception.Message}";
        }
    }

    private async IAsyncEnumerable<Delta> StreamAsync(
        OpenAiChatOptions settings, JsonArray messages, JsonArray tools, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        var body = new JsonObject { ["model"] = settings.Model, ["stream"] = true, ["messages"] = JsonNode.Parse(messages.ToJsonString()) };
        if (tools.Count > 0)
        {
            body["tools"] = JsonNode.Parse(tools.ToJsonString());
        }

        using var http = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            http.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }

        using var response = await httpClients.CreateClient(HttpClientName)
            .SendAsync(http, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
        while (await reader.ReadLineAsync(timeout.Token) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data.Length == 0)
            {
                continue;
            }

            if (data == "[DONE]")
            {
                yield break;
            }

            using var chunk = JsonDocument.Parse(data);
            if (!chunk.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } piece)
            {
                yield return new Delta(piece, null, null, null, null);
            }

            if (delta.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in calls.EnumerateArray())
                {
                    var function = call.TryGetProperty("function", out var f) ? f : default;
                    yield return new Delta(
                        null,
                        call.TryGetProperty("index", out var index) && index.ValueKind == JsonValueKind.Number ? index.GetInt32() : 0,
                        call.TryGetProperty("id", out var id) ? id.GetString() : null,
                        function.ValueKind == JsonValueKind.Object && function.TryGetProperty("name", out var name) ? name.GetString() : null,
                        function.ValueKind == JsonValueKind.Object && function.TryGetProperty("arguments", out var arguments) ? arguments.GetString() : null);
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The model or the gateway did not answer.")]
    private partial void LogFailed(Exception exception);

    private readonly record struct Delta(string? Text, int? CallIndex, string? CallId, string? CallName, string? CallArguments);

    private sealed class ToolCallBuilder
    {
        public string Id { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public StringBuilder Arguments { get; } = new();

        public void Add(Delta delta)
        {
            Id = delta.CallId ?? Id;
            Name = delta.CallName ?? Name;
            Arguments.Append(delta.CallArguments);
        }
    }
}

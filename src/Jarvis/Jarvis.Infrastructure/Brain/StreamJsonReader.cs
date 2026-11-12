using System.Text.Json;
using Jarvis.Application.Abstractions;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// Reads Claude Code's <c>--output-format stream-json</c>, one JSON object per line: <c>system/init</c> names the model,
/// <c>assistant</c> messages carry text blocks, and the closing <c>result</c> has the whole answer and the cost.
/// Everything else (tool use, tool results) is the gateway's business and is skipped.
/// </summary>
internal sealed class StreamJsonReader
{
    private string? _model;

    public IEnumerable<BrainEvent> Read(string line)
    {
        // Anything that is not one JSON object per line (a warning on stdout) is skipped.
        using var json = Parse(line);
        if (json is null)
        {
            yield break;
        }

        var root = json.RootElement;
        switch (Text(root, "type"))
        {
            case "system" when Text(root, "model") is { } model:
                _model = model;
                break;
            case "assistant" when root.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content):
                foreach (var block in content.EnumerateArray())
                {
                    if (Text(block, "type") == "text" && Text(block, "text") is { Length: > 0 } text)
                    {
                        yield return new BrainText(text);
                    }
                }

                break;
            case "result":
                var cost = root.TryGetProperty("total_cost_usd", out var usd) && usd.ValueKind == JsonValueKind.Number ? usd.GetDecimal() : (decimal?)null;
                var failed = root.TryGetProperty("is_error", out var error) && error.ValueKind == JsonValueKind.True;
                if (!failed && Text(root, "result") is { } answer)
                {
                    yield return new BrainFinished(answer, _model, cost);
                }

                break;
        }
    }

    private static JsonDocument? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

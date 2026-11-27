using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Jarvis.Api.Tests;

/// <summary>An OpenAI-compatible endpoint in memory: each call to <c>/chat/completions</c> gets the next scripted SSE answer.</summary>
internal sealed class FakeModel(IEnumerable<string>? answers = null) : HttpMessageHandler
{
    private readonly Queue<string> _answers = new(answers ?? []);

    /// <summary>The body of every request, parsed.</summary>
    public List<JsonNode> Requests { get; } = [];

    /// <summary>When set, answers like a model that hangs.</summary>
    public bool Hang { get; set; }

    public Func<string>? Repeat { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Hang)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (request.Content is not null)
        {
            Requests.Add(JsonNode.Parse(await request.Content.ReadAsStringAsync(cancellationToken))!);
        }

        var sse = _answers.Count > 0 ? _answers.Dequeue() : Repeat?.Invoke() ?? Text("koniec");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sse, Encoding.UTF8, "text/event-stream") };
    }

    public static string Text(params string[] pieces) =>
        string.Concat(pieces.Select(p => Chunk(new JsonObject { ["content"] = p }))) + "data: [DONE]\n\n";

    /// <summary>A tool call whose arguments arrive in two pieces, like a real model streams them.</summary>
    public static string Call(string id, string name, string arguments)
    {
        var half = arguments.Length / 2;
        return Chunk(new JsonObject { ["tool_calls" 
            ] = new JsonArray(new JsonObject { ["index"] = 0, ["id"] = id, ["type"] = "function", ["function"] = new JsonObject { ["name"] = name, ["arguments"] = arguments[..half] } }) })
            + Chunk(new JsonObject { ["tool_calls"] = new JsonArray(new JsonObject { ["index"] = 0, ["function"] = new JsonObject { ["arguments"] = arguments[half..] } }) })
            + "data: [DONE]\n\n";
    }

    private static string Chunk(JsonObject delta) =>
        $"data: {new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["delta"] = delta }) }.ToJsonString()}\n\n";
}

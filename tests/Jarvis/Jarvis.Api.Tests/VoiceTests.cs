using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Api.Voice;
using Jarvis.Application.Voice;

namespace Jarvis.Api.Tests;

/// <summary>JARVIS's ears and voice over <c>/jarvis/voice</c>, the way the Mac bridge uses them (jarvis-voice-bridge.md).</summary>
[Collection(nameof(JarvisCollection))]
public sealed class VoiceTests(JarvisFactory factory)
{
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task A_spoken_request_is_answered_aloud_sentence_by_sentence_and_kept_in_the_conversation()
    {
        factory.Brain.Script = _ => Task.FromResult("Dodałem mleko. Coś jeszcze?");
        factory.Ears.Heard.Enqueue("dodaj mleko do zakupów");
        using var socket = await ConnectAsync();

        var heard = await SpeakAsync(socket);

        Assert.Equal(["transcript", "reply", "audio", "wav:Dodałem mleko.", "audio", "wav:Coś jeszcze?", "done"], heard.Select(Kind));
        Assert.Equal(3200, factory.Ears.LastBytes);
        Assert.Contains("rozmowa jest głosowa", factory.Brain.LastSystemPrompt, StringComparison.Ordinal);
        var conversation = heard[1].GetProperty("conversationId").GetGuid();
        var messages = await factory.CreateAuthenticatedClient(_person).GetStringAsync($"/jarvis/api/conversations/{conversation}/messages");
        Assert.Contains("dodaj mleko do zakupów", messages, StringComparison.Ordinal);
        Assert.Contains("Dodałem mleko. Coś jeszcze?", messages, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_short_yes_after_the_question_confirms_the_waiting_action_without_the_model()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;
        factory.Ears.Heard.Enqueue("usuń mleko z listy");
        factory.Ears.Heard.Enqueue("Tak, potwierdzam.");
        using var socket = await ConnectAsync();

        var asked = (await SpeakAsync(socket)).Single(m => Kind(m) == "reply");
        var runs = factory.Brain.Runs;
        var confirmed = (await SpeakAsync(socket)).Single(m => Kind(m) == "reply");

        Assert.True(asked.GetProperty("awaiting").GetBoolean());
        Assert.Equal("NeedsConfirmation", asked.GetProperty("actions")[0].GetProperty("status").GetString());
        Assert.Equal(runs, factory.Brain.Runs);
        Assert.False(confirmed.GetProperty("awaiting").GetBoolean());
        Assert.Equal("Done", confirmed.GetProperty("actions")[0].GetProperty("status").GetString());
        Assert.StartsWith("Zrobione: Deleted", confirmed.GetProperty("text").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_longer_sentence_after_the_question_is_a_new_request()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;
        factory.Ears.Heard.Enqueue("usuń mleko z listy");
        factory.Ears.Heard.Enqueue("nie wiem, co kupić na obiad");
        using var socket = await ConnectAsync();

        await SpeakAsync(socket);
        var runs = factory.Brain.Runs;
        factory.Brain.Script = _ => Task.FromResult("Może pierogi?");
        var next = (await SpeakAsync(socket)).Single(m => Kind(m) == "reply");

        Assert.Equal(runs + 1, factory.Brain.Runs);
        Assert.Equal("Może pierogi?", next.GetProperty("text").GetString());
    }

    [Fact]
    public async Task A_listener_with_its_own_speech_sends_text_and_gets_text_without_audio()
    {
        factory.Brain.Script = _ => Task.FromResult("Dodałem mleko. Coś jeszcze?");
        using var socket = await ConnectAsync("?speech=local");

        await SendAsync(socket, new { type = "say", text = "dodaj mleko do zakupów" });
        var heard = await UntilDoneAsync(socket);

        Assert.Equal(["transcript", "reply", "done"], heard.Select(Kind));
        Assert.Equal("dodaj mleko do zakupów", heard[0].GetProperty("text").GetString());
        Assert.Equal("Dodałem mleko. Coś jeszcze?", heard[1].GetProperty("text").GetString());

        await SendAsync(socket, new { type = "start" });
        Assert.Equal("voice.speech_local", (await ReceiveAsync(socket)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Silence_gets_an_empty_transcript_and_no_answer()
    {
        using var socket = await ConnectAsync();

        var heard = await SpeakAsync(socket);

        Assert.Equal(["transcript", "done"], heard.Select(Kind));
        Assert.Equal(string.Empty, heard[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task Without_a_token_the_socket_is_refused()
    {
        var client = factory.Server.CreateWebSocketClient();

        var refused = await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync(new Uri("ws://localhost/jarvis/voice"), CancellationToken.None));

        Assert.Contains("401", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tak", true)]
    [InlineData("Tak, potwierdzam.", true)]
    [InlineData("zrób to, Jarvis", true)]
    [InlineData("nie", false)]
    [InlineData("Nie, nie rób tego!", false)]
    [InlineData("anuluj", false)]
    [InlineData("nie wiem, co kupić", null)]
    [InlineData("tak, i dodaj jeszcze chleb", null)]
    [InlineData("", null)]
    public void A_short_answer_is_consent_and_anything_else_is_not(string said, bool? expected) =>
        Assert.Equal(expected, Consent.Of(said));

    [Fact]
    public void Answers_are_spoken_sentence_by_sentence() =>
        Assert.Equal(["Dodałem mleko.", "Coś jeszcze?", "Tak!"], VoiceSession.Sentences("Dodałem mleko. Coś jeszcze?  Tak!"));

    /// <summary>Connects like the bridge: an app token from "Połączone aplikacje" for the gateway's resource.</summary>
    private async Task<WebSocket> ConnectAsync(string query = "")
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request =>
            request.Headers.Authorization = $"Bearer {TestJwt.CreateAppToken(_person, JarvisFactory.Resource, "voice-bridge")}";
        var socket = await client.ConnectAsync(new Uri("ws://localhost/jarvis/voice" + query), CancellationToken.None);
        await SendAsync(socket, new { type = "hello", device = "test" });
        Assert.Equal("ready", Kind(await ReceiveAsync(socket)));
        return socket;
    }

    /// <summary>One turn: start, 0.1 s of audio, stop, and everything the server says until <c>done</c>.</summary>
    private static async Task<List<JsonElement>> SpeakAsync(WebSocket socket)
    {
        await SendAsync(socket, new { type = "start" });
        await socket.SendAsync(new byte[3200], WebSocketMessageType.Binary, true, CancellationToken.None);
        await SendAsync(socket, new { type = "stop" });
        return await UntilDoneAsync(socket);
    }

    private static async Task<List<JsonElement>> UntilDoneAsync(WebSocket socket)
    {
        var heard = new List<JsonElement>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (heard.Count == 0 || Kind(heard[^1]) is not ("done" or "error"))
        {
            heard.Add(await ReceiveAsync(socket, timeout.Token));
        }

        return heard;
    }

    private static Task SendAsync(WebSocket socket, object message) =>
        socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message), WebSocketMessageType.Text, true, CancellationToken.None);

    /// <summary>A text frame as JSON; a binary one (the fake "WAV") as <c>{"type":"wav:&lt;sentence&gt;"}</c>.</summary>
    private static async Task<JsonElement> ReceiveAsync(WebSocket socket, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        WebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(buffer, cancellationToken);
            message.Write(buffer, 0, received.Count);
        }
        while (!received.EndOfMessage);

        return received.MessageType == WebSocketMessageType.Binary
            ? JsonSerializer.SerializeToElement(new { type = "wav:" + Encoding.UTF8.GetString(message.ToArray()) })
            : JsonSerializer.Deserialize<JsonElement>(message.ToArray());
    }

    private static string Kind(JsonElement message) => message.GetProperty("type").GetString()!;
}

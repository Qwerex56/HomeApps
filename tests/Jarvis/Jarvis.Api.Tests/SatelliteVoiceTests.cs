using System.Net.WebSockets;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Api.Tests;

/// <summary>
/// jarvis-satellites.md S4 and S13: a household satellite speaks for itself in its household, and "tu Adrian, PIN 1234"
/// lends one turn to that person. The PIN reaches neither the model nor the listener.
/// </summary>
[Collection(nameof(JarvisCollection))]
public sealed class SatelliteVoiceTests(JarvisFactory factory)
{
    private readonly Guid _device = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _adrian = Guid.NewGuid();

    [Fact]
    public async Task A_turn_with_a_PIN_is_answered_as_the_person_and_not_kept_on_the_satellite()
    {
        factory.Speakers.Pins["4821"] = (_adrian, "Adrian");
        factory.Brain.Script = _ => Task.FromResult("Masz dziś dentystę o 15.");
        using var socket = await ConnectAsync();

        var heard = await SayAsync(socket, "Tu Adrian, PIN 4821, co mam dziś w planie?");

        Assert.Equal(["transcript", "reply", "done"], heard.Select(Kind));
        Assert.DoesNotContain("4821", heard[0].GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Equal("Masz dziś dentystę o 15.", heard[1].GetProperty("text").GetString());
        Assert.Equal(JsonValueKind.Null, heard[1].GetProperty("conversationId").ValueKind);
        Assert.Equal(_adrian, factory.Delegation.Calls.Last().UserId);
        Assert.Contains("co mam dziś w planie?", factory.Brain.LastPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("4821", factory.Brain.LastPrompt, StringComparison.Ordinal);
        Assert.Equal((_device, _household, "Adrian"), factory.Speakers.Asked.Last());
    }

    [Fact]
    public async Task After_the_introduction_the_next_turns_are_the_persons_until_a_wrong_PIN()
    {
        factory.Speakers.Pins["7310"] = (_adrian, "Adrian");
        factory.Brain.Script = _ => Task.FromResult("Gotowe.");
        using var socket = await ConnectAsync();

        var hello = (await SayAsync(socket, "mówi Adrian, PIN siedem trzy jeden zero")).Single(m => Kind(m) == "reply");
        var runs = factory.Brain.Runs;
        var reminded = (await SayAsync(socket, "przypomnij mi o rachunkach")).Single(m => Kind(m) == "reply");
        var personal = factory.Delegation.Calls.Last().UserId;
        await SayAsync(socket, "tu Ewa, PIN 1111");
        await SayAsync(socket, "która godzina?");

        Assert.Equal("Słucham, Adrian.", hello.GetProperty("text").GetString());
        Assert.True(hello.GetProperty("awaiting").GetBoolean());
        Assert.False(reminded.GetProperty("awaiting").GetBoolean());
        Assert.Equal(runs + 2, factory.Brain.Runs);
        Assert.Equal(_adrian, personal);
        Assert.Equal(_device, factory.Delegation.Calls.Last().UserId);
    }

    [Fact]
    public async Task A_wrong_PIN_gets_no_answer_from_the_model()
    {
        using var socket = await ConnectAsync();
        var runs = factory.Brain.Runs;

        var reply = (await SayAsync(socket, "tu Adrian, PIN 0000, otwórz garaż")).Single(m => Kind(m) == "reply");

        Assert.Equal("Nie rozpoznaję tego PIN-u.", reply.GetProperty("text").GetString());
        Assert.Equal(runs, factory.Brain.Runs);
    }

    [Fact]
    public async Task Without_a_PIN_the_satellite_speaks_for_itself_in_its_own_household_conversation()
    {
        factory.Brain.Script = _ => Task.FromResult("Jest 21 stopni.");
        using var socket = await ConnectAsync(householdInHello: Guid.NewGuid());

        var reply = (await SayAsync(socket, "jaka jest temperatura w salonie?")).Single(m => Kind(m) == "reply");

        var conversation = reply.GetProperty("conversationId").GetGuid();
        var messages = await factory.CreateClient(TestJwt.CreateDeviceToken(_device, _household))
            .GetStringAsync($"/jarvis/api/conversations/{conversation}/messages");
        Assert.Equal(_device, factory.Delegation.Calls.Last().UserId);
        Assert.Contains(_household.ToString(), factory.Brain.LastPrompt, StringComparison.Ordinal);
        Assert.Contains("jaka jest temperatura w salonie?", messages, StringComparison.Ordinal);
    }

    [Fact]
    public async Task What_a_satellite_heard_goes_after_a_week()
    {
        factory.Brain.Script = _ => Task.FromResult("Dobrze.");
        using var socket = await ConnectAsync();
        var conversation = (await SayAsync(socket, "zapamiętaj, że okno jest otwarte")).Single(m => Kind(m) == "reply")
            .GetProperty("conversationId").GetGuid();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IConversationRepository>()
                .ForgetSatelliteMessagesAsync(DateTimeOffset.UtcNow.AddDays(1), CancellationToken.None);
        }

        var messages = await factory.CreateClient(TestJwt.CreateDeviceToken(_device, _household))
            .GetStringAsync($"/jarvis/api/conversations/{conversation}/messages");
        Assert.DoesNotContain("okno", messages, StringComparison.Ordinal);
    }

    /// <summary>A satellite with its own speech (S1), its device token naming the household; a household in <c>hello</c> does not change it.</summary>
    private async Task<WebSocket> ConnectAsync(Guid? householdInHello = null)
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request =>
            request.Headers.Authorization = $"Bearer {TestJwt.CreateDeviceToken(_device, _household)}";
        var socket = await client.ConnectAsync(new Uri("ws://localhost/jarvis/voice?speech=local"), CancellationToken.None);
        await SendAsync(socket, new { type = "hello", device = "salon", householdId = householdInHello });
        Assert.Equal("ready", Kind(await ReceiveAsync(socket)));
        return socket;
    }

    private static async Task<List<JsonElement>> SayAsync(WebSocket socket, string text)
    {
        await SendAsync(socket, new { type = "say", text });
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

        return JsonSerializer.Deserialize<JsonElement>(message.ToArray());
    }

    private static string Kind(JsonElement message) => message.GetProperty("type").GetString()!;
}

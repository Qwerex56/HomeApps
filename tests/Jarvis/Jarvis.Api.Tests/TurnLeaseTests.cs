using System.Net.WebSockets;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;

namespace Jarvis.Api.Tests;

/// <summary>Q2(A): two computers of one person never answer the same turn.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class TurnLeaseTests(JarvisFactory factory)
{
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task The_second_computer_gets_lease_conflict_while_the_first_one_is_answered_and_can_speak_after_it()
    {
        var release = new TaskCompletionSource<string>();
        factory.Brain.Script = _ => release.Task;
        using var first = await ConnectAsync(_person);
        using var second = await ConnectAsync(_person);

        await SendAsync(first, new { type = "say", text = "dodaj mleko" });
        await Task.Delay(300);
        await SendAsync(second, new { type = "say", text = "dodaj chleb" });
        var conflict = await ReceiveAsync(second);

        Assert.Equal("lease_conflict", conflict.GetProperty("type").GetString());
        Assert.Equal("turn_in_progress", conflict.GetProperty("code").GetString());

        release.SetResult("Gotowe.");
        while ((await ReceiveAsync(first)).GetProperty("type").GetString() != "done")
        {
        }

        factory.Brain.Script = _ => Task.FromResult("Proszę.");
        await SendAsync(second, new { type = "say", text = "dodaj chleb" });
        Assert.Equal("transcript", (await ReceiveAsync(second)).GetProperty("type").GetString());
    }

    private async Task<WebSocket> ConnectAsync(Guid person)
    {
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request =>
            request.Headers.Authorization = $"Bearer {TestJwt.CreateAppToken(person, JarvisFactory.Resource, "voice-bridge")}";
        var socket = await client.ConnectAsync(new Uri("ws://localhost/jarvis/voice?speech=local"), CancellationToken.None);
        await SendAsync(socket, new { type = "hello" });
        Assert.Equal("ready", (await ReceiveAsync(socket)).GetProperty("type").GetString());
        return socket;
    }

    private static Task SendAsync(WebSocket socket, object message) =>
        socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<JsonElement> ReceiveAsync(WebSocket socket)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        ValueWebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(buffer.AsMemory(), timeout.Token);
            message.Write(buffer, 0, received.Count);
        }
        while (!received.EndOfMessage);

        return JsonSerializer.Deserialize<JsonElement>(message.ToArray());
    }
}

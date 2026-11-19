using System.Net.WebSockets;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Abstractions;

namespace Jarvis.Api.Tests;

/// <summary>A household satellite's voice socket with its own device token: connect, say a sentence, hear the answer.</summary>
internal static class SatelliteSocket
{
    public static async Task<WebSocket> ConnectAsync(JarvisFactory factory, Guid device, Guid household)
    {
        factory.Devices.Set.TryAdd(household, [new HouseholdDevice(device, "Salon", "assistant", true, false, false, null, null, null)]);
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization = $"Bearer {TestJwt.CreateDeviceToken(device, household)}";
        var socket = await client.ConnectAsync(new Uri("ws://localhost/jarvis/voice?speech=local"), CancellationToken.None);
        await SendAsync(socket, new { type = "hello", device = "salon" });
        var ready = await ReceiveAsync(socket, CancellationToken.None);
        Assert.Equal("ready", ready.GetProperty("type").GetString());
        return socket;
    }

    public static async Task<List<JsonElement>> SayAsync(WebSocket socket, string text)
    {
        await SendAsync(socket, new { type = "say", text });
        var heard = new List<JsonElement>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (heard.Count == 0 || heard[^1].GetProperty("type").GetString() is not ("done" or "error"))
        {
            heard.Add(await ReceiveAsync(socket, timeout.Token));
        }

        return heard;
    }

    private static Task SendAsync(WebSocket socket, object message) =>
        socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message), WebSocketMessageType.Text, true, CancellationToken.None);

    private static async Task<JsonElement> ReceiveAsync(WebSocket socket, CancellationToken cancellationToken)
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
}

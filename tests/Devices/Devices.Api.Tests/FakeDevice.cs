using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Devices.Api.Tests;

/// <summary>
/// A device on <c>/devices/link</c> with an app token: says hello and answers every action with
/// "<c>{name} done: {input}</c>", or stays silent when <see cref="Silent"/>.
/// </summary>
internal sealed class FakeDevice : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _stop = new();
    private Task _answering = Task.CompletedTask;

    private FakeDevice(WebSocket socket) => _socket = socket;

    public JsonElement First { get; private set; }

    public bool Silent { get; set; }

    public static async Task<FakeDevice> ConnectAsync(WebApplicationFactory<Program> api, Guid owner, string name, params object[] actions)
    {
        var client = api.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Authorization =
            "Bearer " + TestJwt.CreateAppToken(owner, DevicesApiFactory.Resource, "homeapps-voice-bridge");
        var socket = await client.ConnectAsync(new Uri(api.Server.BaseAddress, "devices/link"), CancellationToken.None);
        var device = new FakeDevice(socket);
        await device.SendAsync(new { type = "hello", device = name, kind = "computer", actions });
        device.First = await device.ReceiveAsync();
        if (device.First.GetProperty("type").GetString() == "ready")
        {
            device._answering = device.AnswerAsync();
        }

        return device;
    }

    public static object Action(string name, bool confirm = false, bool readOnly = false, object? input = null) =>
        new { name, description = "Does " + name, confirm, readOnly, input };

    private async Task AnswerAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var message = await ReceiveAsync();
                if (Silent)
                {
                    continue;
                }

                var input = message.TryGetProperty("input", out var given) ? given.GetRawText() : "{}";
                await SendAsync(new
                {
                    type = "action_result",
                    id = message.GetProperty("id").GetString(),
                    ok = true,
                    text = $"{message.GetProperty("name").GetString()} done: {input}",
                });
            }
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or InvalidOperationException or IOException)
        {
            // Closed.
        }
    }

    private Task SendAsync(object message) =>
        _socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message), WebSocketMessageType.Text, true, _stop.Token);

    private async Task<JsonElement> ReceiveAsync()
    {
        var buffer = new byte[16 * 1024];
        var received = await _socket.ReceiveAsync(buffer, _stop.Token);
        if (received.MessageType == WebSocketMessageType.Close)
        {
            throw new InvalidOperationException("Closed.");
        }

        return JsonElement.Parse(Encoding.UTF8.GetString(buffer, 0, received.Count));
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        try
        {
            await _answering;
        }
        catch (OperationCanceledException)
        {
        }

        _socket.Abort();
        _socket.Dispose();
        _stop.Dispose();
    }
}

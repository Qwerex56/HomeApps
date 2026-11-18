using System.Net.WebSockets;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// This computer as a device of the person (jarvis-satellites.md 2a): a WebSocket to <c>/devices/link</c> that announces
/// the <c>--actions</c> and runs what JARVIS asks for. Reconnects every 5 s while the bridge runs.
/// </summary>
internal sealed class DeviceLink(BridgeOptions options, OAuthLogin login, IReadOnlyList<ComputerAction> actions) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _sending = new(1, 1);
    private ClientWebSocket? _socket;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LinkAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or WebSocketException or JsonException)
            {
                Console.WriteLine($"Akcje komputera: brak połączenia ({exception.Message}); próbuję za 5 s.");
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket is { State: WebSocketState.Open })
        {
            await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        _socket?.Dispose();
        _sending.Dispose();
    }

    private async Task LinkAsync(CancellationToken cancellationToken)
    {
        _socket?.Dispose();
        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Authorization", $"Bearer {await login.AccessTokenAsync(cancellationToken)}");
        if (options.Insecure)
        {
            _socket.Options.RemoteCertificateValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors);
        }

        var uri = new UriBuilder(new Uri(options.Server, "devices/link"))
        {
            Scheme = options.Server.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
            Port = options.Server.Port,
        }.Uri;
        await _socket.ConnectAsync(uri, cancellationToken);
        await SendAsync(new
        {
            type = "hello",
            device = options.Device,
            kind = "computer",
            actions = actions.Select(a => new { a.Name, a.Description, a.Input, a.ReadOnly, a.Confirm }),
        }, cancellationToken);

        while (await ReceiveAsync(_socket, cancellationToken) is { } message)
        {
            switch (message.GetProperty("type").GetString())
            {
                case "ready":
                    Console.WriteLine($"Akcje komputera: {actions.Count} gotowe dla JARVIS-a.");
                    break;
                case "error":
                    Console.WriteLine($"Akcje komputera odrzucone: {Text(message, "code")} {Text(message, "text")}".TrimEnd());
                    return;
                case "action":
                    _ = AnswerAsync(message, cancellationToken);
                    break;
            }
        }
    }

    private async Task AnswerAsync(JsonElement message, CancellationToken cancellationToken)
    {
        var id = Text(message, "id");
        var name = Text(message, "name");
        JsonElement? input = message.TryGetProperty("input", out var given) ? given : null;
        var action = actions.FirstOrDefault(a => a.Name == name);
        Console.WriteLine($"» JARVIS: {name}");
        var (ok, text) = action is null
            ? (false, $"Nie znam akcji {name}.")
            : await CommandRunner.RunAsync(action, input, cancellationToken);
        await SendAsync(new { type = "action_result", id, ok, text }, cancellationToken);
    }

    private static string? Text(JsonElement message, string property) =>
        message.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private async Task SendAsync(object message, CancellationToken cancellationToken)
    {
        await _sending.WaitAsync(cancellationToken);
        try
        {
            if (_socket is { State: WebSocketState.Open } socket)
            {
                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, Json), WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        finally
        {
            _sending.Release();
        }
    }

    private static async Task<JsonElement?> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();
        ValueWebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
            if (received.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, received.Count);
        }
        while (!received.EndOfMessage);

        return received.MessageType == WebSocketMessageType.Text ? JsonSerializer.Deserialize<JsonElement>(message.ToArray()) : null;
    }
}

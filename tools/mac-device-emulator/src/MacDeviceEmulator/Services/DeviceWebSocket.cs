using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MacDeviceEmulator.Protocol;

namespace MacDeviceEmulator.Services;

public interface IDeviceWebSocket : IAsyncDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, string? bearerToken, CancellationToken cancellationToken = default);
    Task SendMessageAsync(LinkMessage message, CancellationToken cancellationToken = default);
    Task<LinkMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default);
}

public sealed class DeviceWebSocket : IDeviceWebSocket
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private ClientWebSocket _socket = new();

    public WebSocketState State => _socket.State;

    public async Task ConnectAsync(Uri uri, string? bearerToken, CancellationToken cancellationToken = default)
    {
        if (_socket.State == WebSocketState.Open)
        {
            return;
        }

        _socket.Dispose();
        _socket = new ClientWebSocket();
        if (!string.IsNullOrEmpty(bearerToken))
        {
            _socket.Options.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
        }

        await _socket.ConnectAsync(uri, cancellationToken);
    }

    public async Task SendMessageAsync(LinkMessage message, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task<LinkMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[64 * 1024];
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        ms.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<LinkMessage>(ms, JsonOptions, cancellationToken);
    }

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken = default)
    {
        return _socket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
            }
            catch
            {
                // Ignore
            }
        }
        _socket.Dispose();
    }
}

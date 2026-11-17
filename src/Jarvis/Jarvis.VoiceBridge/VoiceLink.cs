using System.Buffers.Binary;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The WebSocket to <c>/jarvis/voice</c> (protocol in docs/jarvis.md "Voice"): controls and 16 kHz PCM out, the
/// server's messages in, as <see cref="ServerEvent"/>s. Remembers the conversation, so a reconnect continues it.
/// </summary>
internal sealed class VoiceLink(BridgeOptions options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _sending = new(1, 1);
    private readonly Channel<ServerEvent> _events = Channel.CreateUnbounded<ServerEvent>();
    private ClientWebSocket? _socket;
    private string? _conversationId;

    public ChannelReader<ServerEvent> Events => _events.Reader;

    public bool IsOpen => _socket?.State == WebSocketState.Open;

    public async Task ConnectAsync(string token, CancellationToken cancellationToken)
    {
        _socket?.Dispose();
        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        if (options.Insecure)
        {
            _socket.Options.RemoteCertificateValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors);
        }

        var scheme = options.Server.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";
        var uri = new UriBuilder(new Uri(options.Server, "jarvis/voice")) { Scheme = scheme, Port = options.Server.Port }.Uri;
        await _socket.ConnectAsync(uri, cancellationToken);
        await SendAsync(new { type = "hello", device = options.Device, conversationId = _conversationId }, cancellationToken);
        _ = ReceiveAsync(_socket, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken) => SendAsync(new { type = "start" }, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => SendAsync(new { type = "stop" }, cancellationToken);

    public Task CancelAsync(CancellationToken cancellationToken) => SendAsync(new { type = "cancel" }, cancellationToken);

    public Task SendAudioAsync(short[] frame, CancellationToken cancellationToken)
    {
        var bytes = new byte[frame.Length * 2];
        for (var i = 0; i < frame.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2), frame[i]);
        }

        return SendAsync(bytes, WebSocketMessageType.Binary, cancellationToken);
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

    private Task SendAsync(object message, CancellationToken cancellationToken) =>
        SendAsync(JsonSerializer.SerializeToUtf8Bytes(message), WebSocketMessageType.Text, cancellationToken);

    private async Task SendAsync(byte[] bytes, WebSocketMessageType type, CancellationToken cancellationToken)
    {
        await _sending.WaitAsync(cancellationToken);
        try
        {
            if (IsOpen)
            {
                await _socket!.SendAsync(bytes, type, endOfMessage: true, cancellationToken);
            }
        }
        finally
        {
            _sending.Release();
        }
    }

    private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
        ServerEvent? audio = null;
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                message.SetLength(0);
                ValueWebSocketReceiveResult received;
                do
                {
                    received = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
                    message.Write(buffer, 0, received.Count);
                }
                while (!received.EndOfMessage && received.MessageType != WebSocketMessageType.Close);

                if (received.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (received.MessageType == WebSocketMessageType.Binary)
                {
                    if (audio is not null)
                    {
                        await _events.Writer.WriteAsync(audio with { Wav = message.ToArray() }, cancellationToken);
                        audio = null;
                    }

                    continue;
                }

                var json = JsonSerializer.Deserialize<JsonElement>(message.ToArray());
                var said = new ServerEvent(json.GetProperty("type").GetString() ?? string.Empty, json);
                if (said.Text("conversationId") is { } conversation)
                {
                    _conversationId = conversation;
                }

                if (said.Type == "audio")
                {
                    audio = said;
                    continue;
                }

                await _events.Writer.WriteAsync(said, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
        {
            // Closed: the bridge sees the link is down and reconnects.
        }

        await _events.Writer.WriteAsync(new ServerEvent("closed", default), CancellationToken.None);
    }
}

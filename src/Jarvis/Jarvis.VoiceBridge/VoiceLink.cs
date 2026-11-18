using System.Buffers.Binary;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The WebSocket to <c>/jarvis/voice</c> (protocol in docs/jarvis.md "Voice"): controls and 16 kHz PCM out, the
/// server's messages in, as <see cref="ServerEvent"/>s. Remembers the conversation, so a reconnect continues it.
/// With <paramref name="speech"/> (S1) the audio stays here: it is transcribed locally and sent as <c>say</c>, and the
/// reply is spoken locally, as the same <c>audio</c> events the bridge plays.
/// </summary>
internal sealed partial class VoiceLink(BridgeOptions options, LocalSpeech? speech = null) : IAsyncDisposable
{
    private readonly List<short> _heard = [];
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
        var uri = new UriBuilder(new Uri(options.Server, "jarvis/voice"))
        {
            Scheme = scheme,
            Port = options.Server.Port,
            Query = speech is null ? string.Empty : "speech=local",
        }.Uri;
        await _socket.ConnectAsync(uri, cancellationToken);
        await SendAsync(new { type = "hello", device = options.Device, conversationId = _conversationId }, cancellationToken);
        _ = ReceiveAsync(_socket, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (speech is not null)
        {
            lock (_heard)
            {
                _heard.Clear();
            }

            return Task.CompletedTask;
        }

        return SendAsync(new { type = "start" }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (speech is not null)
        {
            short[] heard;
            lock (_heard)
            {
                heard = [.. _heard];
                _heard.Clear();
            }

            _ = SayAsync(speech, heard, cancellationToken);
            return Task.CompletedTask;
        }

        return SendAsync(new { type = "stop" }, cancellationToken);
    }

    public Task CancelAsync(CancellationToken cancellationToken)
    {
        lock (_heard)
        {
            _heard.Clear();
        }

        return SendAsync(new { type = "cancel" }, cancellationToken);
    }

    public Task SendAudioAsync(short[] frame, CancellationToken cancellationToken)
    {
        if (speech is not null)
        {
            lock (_heard)
            {
                _heard.AddRange(frame);
            }

            return Task.CompletedTask;
        }

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

    /// <summary>S1: hears the utterance here; nothing heard ends the turn without asking the server.</summary>
    private async Task SayAsync(LocalSpeech local, short[] heard, CancellationToken cancellationToken)
    {
        try
        {
            var text = heard.Length == 0 ? string.Empty : await local.TranscribeAsync(heard, cancellationToken);
            if (text.Length == 0)
            {
                await LocalEventAsync(new { type = "transcript", text }, cancellationToken);
                await LocalEventAsync(new { type = "done" }, cancellationToken);
                return;
            }

            await SendAsync(new { type = "say", text }, cancellationToken);
        }
        catch (HttpRequestException)
        {
            await LocalEventAsync(new { type = "error", code = "voice.engine_unavailable", detail = options.Engine?.ToString() }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Quitting.
        }
    }

    /// <summary>S1: JARVIS's answer spoken here, sentence by sentence, as the server would have sent it.</summary>
    private async Task SpeakAsync(LocalSpeech local, string text, CancellationToken cancellationToken)
    {
        foreach (var sentence in Sentence().Split(text.Trim()).Where(s => s.Length > 0))
        {
            try
            {
                var wav = await local.SpeakAsync(sentence, cancellationToken);
                var said = JsonSerializer.SerializeToElement(new { type = "audio", format = "wav", text = sentence });
                await _events.Writer.WriteAsync(new ServerEvent("audio", said, wav), cancellationToken);
            }
            catch (HttpRequestException)
            {
                await LocalEventAsync(new { type = "error", code = "voice.engine_unavailable", detail = options.Engine?.ToString() }, cancellationToken);
                return;
            }
        }
    }

    private ValueTask LocalEventAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToElement(message);
        return _events.Writer.WriteAsync(new ServerEvent(json.GetProperty("type").GetString()!, json), cancellationToken);
    }

    [GeneratedRegex(@"(?<=[.!?…])\s+")]
    private static partial Regex Sentence();

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
                if (speech is not null && said.Type == "reply" && said.Text("text") is { Length: > 0 } reply)
                {
                    await SpeakAsync(speech, reply, cancellationToken);
                }
            }
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
        {
            // Closed: the bridge sees the link is down and reconnects.
        }

        await _events.Writer.WriteAsync(new ServerEvent("closed", default), CancellationToken.None);
    }
}

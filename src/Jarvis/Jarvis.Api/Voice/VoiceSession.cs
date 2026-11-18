using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Voice;
using Jarvis.Application.Voice.VoiceTurn;
using MediatR;

namespace Jarvis.Api.Voice;

/// <summary>
/// One listener's connection. Text frames are control messages (<c>hello</c>, <c>start</c>, <c>stop</c>,
/// <c>cancel</c>), binary frames are 16 kHz 16-bit mono PCM between <c>start</c> and <c>stop</c>. A turn runs while the
/// socket is still read, so <c>cancel</c> (or a new <c>start</c>) stops JARVIS mid-answer. The answer comes back as
/// <c>transcript</c>, <c>reply</c>, then <c>audio</c> + one WAV per sentence, then <c>done</c>; or <c>error</c>.
/// The session remembers the conversation and what waits for "tak"/"nie"; the audio is dropped after each turn.
/// With <paramref name="local"/> (<c>?speech=local</c>, S1) the listener hears and speaks itself: it sends
/// <c>say</c> with the text and gets <c>transcript</c>, <c>reply</c> and <c>done</c> without audio.
/// </summary>
internal sealed partial class VoiceSession(
    WebSocket socket, Guid userId, bool local, IServiceScopeFactory scopes, VoiceOptions options, ILogger<VoiceSession> logger) : IDisposable
{
    public const int SampleRate = 16_000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly SemaphoreSlim _sending = new(1, 1);
    private readonly MemoryStream _audio = new();
    private Guid? _conversationId;
    private Guid? _householdId;
    private IReadOnlyList<Guid> _pending = [];
    private bool _recording;
    private CancellationTokenSource? _turn;
    private Task _running = Task.CompletedTask;

    private int MaxBytes => options.MaxSeconds * SampleRate * 2;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();
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
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;
                }

                if (received.MessageType == WebSocketMessageType.Binary)
                {
                    if (_recording && _audio.Length + message.Length <= MaxBytes)
                    {
                        message.WriteTo(_audio);
                    }

                    continue;
                }

                await OnControlAsync(message.ToArray(), cancellationToken);
            }
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
        {
            // The listener went away.
        }
        finally
        {
            await StopTurnAsync();
        }
    }

    public void Dispose()
    {
        _turn?.Dispose();
        _sending.Dispose();
        _audio.Dispose();
    }

    private async Task OnControlAsync(byte[] text, CancellationToken cancellationToken)
    {
        Control? control;
        try
        {
            control = JsonSerializer.Deserialize<Control>(text, Json);
        }
        catch (JsonException)
        {
            control = null;
        }

        switch (control?.Type)
        {
            case "hello":
                _conversationId = control.ConversationId ?? _conversationId;
                _householdId = control.HouseholdId;
                await SendAsync(new { type = "ready", conversationId = _conversationId }, CancellationToken.None);
                break;
            case "start" or "stop" when local:
                await SendAsync(new { type = "error", code = "voice.speech_local" }, CancellationToken.None);
                break;
            case "say":
                await StopTurnAsync();
                _turn?.Dispose();
                _turn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _running = TurnAsync([], control.Text ?? string.Empty, _turn.Token);
                break;
            case "start":
                await StopTurnAsync();
                _audio.SetLength(0);
                _recording = true;
                break;
            case "stop":
                _recording = false;
                await StopTurnAsync();
                var audio = _audio.ToArray();
                _audio.SetLength(0);
                _turn?.Dispose();
                _turn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _running = TurnAsync(audio, null, _turn.Token);
                break;
            case "cancel":
                _recording = false;
                _audio.SetLength(0);
                await StopTurnAsync();
                break;
            default:
                await SendAsync(new { type = "error", code = "voice.unknown_message" }, CancellationToken.None);
                break;
        }
    }

    private async Task TurnAsync(byte[] audio, string? text, CancellationToken cancellationToken)
    {
        if (text is null ? audio.Length == 0 : string.IsNullOrWhiteSpace(text))
        {
            await SendAsync(new { type = "transcript", text = string.Empty }, cancellationToken);
            await SendAsync(new { type = "done" }, cancellationToken);
            return;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new VoiceTurnCommand(userId, _conversationId, _householdId, audio, SampleRate, _pending, text), cancellationToken);
            if (result.IsFailed)
            {
                var error = result.Errors.OfType<AppError>().FirstOrDefault();
                await SendAsync(new { type = "error", code = error?.Code ?? "voice.error", detail = error?.Message }, cancellationToken);
                return;
            }

            var turn = result.Value;
            _conversationId = turn.ConversationId;
            _pending = turn.Pending;
            await SendAsync(new { type = "transcript", text = turn.Transcript }, cancellationToken);
            if (turn.Answer is { } answer)
            {
                await SendAsync(new
                {
                    type = "reply",
                    conversationId = _conversationId,
                    text = answer.Text,
                    actions = answer.Actions,
                    awaiting = _pending.Count > 0,
                }, cancellationToken);

                var voice = scope.ServiceProvider.GetRequiredService<ITextToSpeech>();
                foreach (var sentence in local ? [] : Sentences(answer.Text))
                {
                    var wav = await voice.SpeakAsync(sentence, cancellationToken);
                    await SendAsync(new { type = "audio", format = "wav", text = sentence }, cancellationToken);
                    await SendAsync(wav, WebSocketMessageType.Binary, cancellationToken);
                }
            }

            await SendAsync(new { type = "done" }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await SendAsync(new { type = "done", cancelled = true }, CancellationToken.None);
        }
        catch (HttpRequestException exception)
        {
            LogEngineFailed(exception);
            await SendAsync(new { type = "error", code = "voice.engine_unavailable" }, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not WebSocketException)
        {
            LogTurnFailed(exception);
            await SendAsync(new { type = "error", code = "voice.error" }, CancellationToken.None);
        }
    }

    private async Task StopTurnAsync()
    {
        if (_running.IsCompleted)
        {
            return;
        }

        await (_turn?.CancelAsync() ?? Task.CompletedTask);
        try
        {
            await _running;
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
        {
            // Cancelled on purpose, or the listener is gone.
        }
    }

    /// <summary>Sentence by sentence, so the first one is spoken while the next is synthesised.</summary>
    internal static IEnumerable<string> Sentences(string text) =>
        SentenceEnd().Split(text).Select(s => s.Trim()).Where(s => s.Length > 0);

    private Task SendAsync(object message, CancellationToken cancellationToken) =>
        SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, Json), WebSocketMessageType.Text, cancellationToken);

    private async Task SendAsync(byte[] bytes, WebSocketMessageType type, CancellationToken cancellationToken)
    {
        await _sending.WaitAsync(cancellationToken);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(bytes, type, endOfMessage: true, cancellationToken);
            }
        }
        finally
        {
            _sending.Release();
        }
    }

    [GeneratedRegex(@"(?<=[.!?…])\s+")]
    private static partial Regex SentenceEnd();

    [LoggerMessage(Level = LogLevel.Warning, Message = "A speech engine did not answer.")]
    private partial void LogEngineFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "A voice turn failed.")]
    private partial void LogTurnFailed(Exception exception);

    private sealed record Control(string? Type, Guid? ConversationId, Guid? HouseholdId, string? Text);
}

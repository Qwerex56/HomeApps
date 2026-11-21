using System.Net.WebSockets;
using FluentResults;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Protocol;

namespace MacDeviceEmulator.Services;

/// <summary>
/// Orchestrates H-8 continuous local listening:
/// local input -> local "is this for me?" / lock / mute / echo / screen-confirmation gate
/// -> <c>/jarvis/voice?speech=local</c> -> local TTS with self-voice echo suppression.
/// </summary>
public sealed class ContinuousListeningService : IAsyncDisposable
{
    private readonly string _serverUrl;
    private readonly string? _userToken;
    private readonly Func<IVoiceWebSocket> _voiceWebSocketFactory;
    private readonly IMacSystem _macSystem;
    private readonly ISessionLockMonitor _sessionLockMonitor;
    private readonly ISpeechEchoSuppressor _echoSuppressor;
    private readonly IUtteranceGate _utteranceGate;
    private readonly IListeningIndicator _indicator;
    private readonly IHomeAppsClient _homeAppsClient;
    private readonly TimeProvider _timeProvider;
    private readonly TextWriter _output;
    private readonly TimeSpan _lockPollInterval;
    private readonly TimeSpan _turnTimeout;
    private readonly SemaphoreSlim _turnLock = new(1, 1);
    private readonly Lock _stateGate = new();

    private IVoiceWebSocket? _voiceSocket;
    private bool _isMuted;
    private bool? _manualLockOverride;
    private Task _activeSpeechTask = Task.CompletedTask;

    public ContinuousListeningService(
        string serverUrl,
        string? userToken,
        Func<IVoiceWebSocket> voiceWebSocketFactory,
        IMacSystem macSystem,
        ISessionLockMonitor sessionLockMonitor,
        ISpeechEchoSuppressor echoSuppressor,
        IUtteranceGate utteranceGate,
        IListeningIndicator indicator,
        IHomeAppsClient homeAppsClient,
        TimeProvider? timeProvider = null,
        bool startMuted = false,
        TextWriter? output = null,
        TimeSpan? lockPollInterval = null,
        TimeSpan? turnTimeout = null)
    {
        _serverUrl = serverUrl;
        _userToken = userToken;
        _voiceWebSocketFactory = voiceWebSocketFactory;
        _macSystem = macSystem;
        _sessionLockMonitor = sessionLockMonitor;
        _echoSuppressor = echoSuppressor;
        _utteranceGate = utteranceGate;
        _indicator = indicator;
        _homeAppsClient = homeAppsClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _isMuted = startMuted;
        _output = output ?? Console.Out;
        _lockPollInterval = lockPollInterval ?? TimeSpan.FromSeconds(2);
        _turnTimeout = turnTimeout ?? TimeSpan.FromSeconds(60);
    }

    public Guid? ConversationId { get; private set; }

    public PendingConfirmationStore PendingConfirmations { get; } = new();

    public bool IsMuted
    {
        get
        {
            lock (_stateGate)
            {
                return _isMuted;
            }
        }
    }

    public bool? ManualLockOverride
    {
        get
        {
            lock (_stateGate)
            {
                return _manualLockOverride;
            }
        }
    }

    public void SetMuted(bool muted)
    {
        lock (_stateGate)
        {
            _isMuted = muted;
        }
    }

    public bool ToggleMute()
    {
        bool next;
        lock (_stateGate)
        {
            _isMuted = !_isMuted;
            next = _isMuted;
        }

        return next;
    }

    public void SetManualLock(bool? locked)
    {
        lock (_stateGate)
        {
            _manualLockOverride = locked;
        }
    }

    public async Task<bool> IsEffectivelyLockedAsync(CancellationToken cancellationToken = default)
    {
        bool? manual;
        lock (_stateGate)
        {
            manual = _manualLockOverride;
        }

        if (manual.HasValue)
        {
            return manual.Value;
        }

        return await _sessionLockMonitor.IsLockedAsync(cancellationToken);
    }

    public async Task<ListeningStatus> RefreshIndicatorAsync(CancellationToken cancellationToken = default)
    {
        var locked = await IsEffectivelyLockedAsync(cancellationToken);
        var status = ComputeStatus(locked, IsMuted, _echoSuppressor.IsSuppressing);
        _indicator.SetStatus(status);
        return status;
    }

    public async Task RunAsync(IReadOnlyList<ISpeechInput> speechInputs, CancellationToken cancellationToken)
    {
        await RefreshIndicatorAsync(cancellationToken);

        var tasks = new List<Task>
        {
            PollSessionLockAsync(cancellationToken)
        };

        foreach (var input in speechInputs)
        {
            tasks.Add(ReadInputLoopAsync(input, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            try
            {
                await _activeSpeechTask;
            }
            catch
            {
                // Best effort wait for active speech on shutdown
            }

            _indicator.SetStatus(ListeningStatus.Stopped);
        }
    }

    public async Task<GateDecision> ProcessUtteranceAsync(string? transcript, CancellationToken cancellationToken = default)
    {
        await _turnLock.WaitAsync(cancellationToken);
        try
        {
            var locked = await IsEffectivelyLockedAsync(cancellationToken);
            var muted = IsMuted;
            var isEcho = !string.IsNullOrWhiteSpace(transcript) && _echoSuppressor.IsEcho(transcript);
            var hasPending = PendingConfirmations.HasPending(_timeProvider.GetUtcNow());

            _indicator.SetStatus(ComputeStatus(locked, muted, _echoSuppressor.IsSuppressing));

            var decision = _utteranceGate.Evaluate(
                transcript,
                new GateContext(muted, locked, isEcho, hasPending));

            if (!decision.Accepted)
            {
                LogRejection(transcript, decision.Reason);
                return decision;
            }

            await SendToJarvisLockedAsync(decision.CommandText!, cancellationToken);
            return decision;
        }
        finally
        {
            _turnLock.Release();
        }
    }

    public async Task<Result<VoiceActionView>> DecideConfirmationOnScreenAsync(
        Guid actionId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_userToken))
        {
            return Result.Fail("No user Bearer token provided for confirmation decision.");
        }

        var result = await _homeAppsClient.DecideConfirmationAsync(_serverUrl, _userToken, actionId, approve, cancellationToken);
        if (result.IsSuccess)
        {
            PendingConfirmations.Remove(actionId);
            _output.WriteLine($"[Confirmation] Action {actionId} {(approve ? "approved" : "denied")}: {result.Value.Description} ({result.Value.Status})");
        }
        else
        {
            var err = result.Errors.First().Message;
            if (err.Contains("NotFound", StringComparison.OrdinalIgnoreCase) || err.Contains("Expired", StringComparison.OrdinalIgnoreCase))
            {
                PendingConfirmations.Remove(actionId);
            }

            _output.WriteLine($"[Confirmation] Failed to {(approve ? "approve" : "deny")} {actionId}: {err}");
        }

        return result;
    }

    public async Task WaitForSpeechAsync()
    {
        Task pending;
        lock (_stateGate)
        {
            pending = _activeSpeechTask;
        }

        await pending;
    }

    public static Uri BuildVoiceUri(string serverUrl)
    {
        var trimmed = serverUrl.TrimEnd('/');
        string wsBase;
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            wsBase = "wss://" + trimmed["https://".Length..];
        }
        else if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            wsBase = "ws://" + trimmed["http://".Length..];
        }
        else
        {
            wsBase = trimmed;
        }

        return new Uri($"{wsBase}/jarvis/voice?speech=local");
    }

    public async ValueTask DisposeAsync()
    {
        if (_voiceSocket is not null)
        {
            await _voiceSocket.DisposeAsync();
            _voiceSocket = null;
        }

        _turnLock.Dispose();
    }

    private async Task ReadInputLoopAsync(ISpeechInput input, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            RecognizedSpeech? speech;
            try
            {
                speech = await input.ReadNextAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (speech is null)
            {
                break;
            }

            try
            {
                await ProcessUtteranceAsync(speech.Text, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _output.WriteLine($"[Listening] Error processing utterance: {ex.Message}");
            }
        }
    }

    private async Task PollSessionLockAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_lockPollInterval, _timeProvider, cancellationToken);
                await RefreshIndicatorAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[Listening] Session lock check error: {ex.Message}");
            }
        }
    }

    private async Task SendToJarvisLockedAsync(string commandText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_userToken))
        {
            _output.WriteLine("[Listening] Cannot send to JARVIS: no user Bearer token configured (--user-token / HOMEAPPS_USER_TOKEN).");
            return;
        }

        using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        turnCts.CancelAfter(_turnTimeout);

        try
        {
            var socket = await EnsureConnectedAsync(turnCts.Token);
            if (socket is null)
            {
                return;
            }

            _output.WriteLine($"[Listening -> JARVIS] Sending: \"{commandText}\"");
            await socket.SendMessageAsync(new VoiceMessage("say", Text: commandText), turnCts.Token);

            var hasNeedsConfirmation = false;
            while (!turnCts.Token.IsCancellationRequested)
            {
                var message = await socket.ReceiveMessageAsync(turnCts.Token);
                if (message is null)
                {
                    _output.WriteLine("[Listening] Voice connection closed by server.");
                    await ResetSocketAsync();
                    break;
                }

                if (message.Type == "transcript")
                {
                    _output.WriteLine($"[Voice] Transcript: \"{message.Text}\"");
                }
                else if (message.Type == "reply")
                {
                    if (message.ConversationId is { } convId)
                    {
                        ConversationId = convId;
                    }

                    if (!string.IsNullOrWhiteSpace(message.Text))
                    {
                        _output.WriteLine($"[JARVIS] {message.Text}");
                    }

                    if (HandleReplyActions(message))
                    {
                        hasNeedsConfirmation = true;
                    }

                    if (!string.IsNullOrWhiteSpace(message.Text))
                    {
                        StartTrackedSpeech(message.Text, cancellationToken);
                    }
                }
                else if (message.Type == "error")
                {
                    _output.WriteLine($"[Voice] Server error: {message.Code} {message.Detail}".Trim());
                    break;
                }
                else if (message.Type == "done")
                {
                    break;
                }
            }

            // Defense in depth: server session's _pending lives per socket. Reset socket after reply with NeedsConfirmation actions
            // so subsequent turns cannot count as voice consent on the server socket.
            if (hasNeedsConfirmation)
            {
                await ResetSocketAsync();
            }
        }
        catch (OperationCanceledException) when (turnCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _output.WriteLine($"[Listening] Voice turn timed out after {_turnTimeout.TotalSeconds:F0}s. Resetting voice connection.");
            await ResetSocketAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _output.WriteLine($"[Listening] Voice turn failed: {ex.Message}");
            await ResetSocketAsync();
        }
    }

    private async Task<IVoiceWebSocket?> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_voiceSocket is { State: WebSocketState.Open })
        {
            return _voiceSocket;
        }

        await ResetSocketAsync();

        var socket = _voiceWebSocketFactory();
        try
        {
            var uri = BuildVoiceUri(_serverUrl);
            await socket.ConnectAsync(uri, _userToken, cancellationToken);
            await socket.SendMessageAsync(new VoiceMessage("hello", ConversationId: ConversationId), cancellationToken);

            var ready = await socket.ReceiveMessageAsync(cancellationToken);
            if (ready?.Type != "ready")
            {
                _output.WriteLine($"[Listening] Expected 'ready' on {uri}, got '{ready?.Type}' ({ready?.Code}).");
                await socket.DisposeAsync();
                return null;
            }

            if (ready.ConversationId is { } convId)
            {
                ConversationId = convId;
            }

            _voiceSocket = socket;
            return socket;
        }
        catch
        {
            try
            {
                await socket.DisposeAsync();
            }
            catch
            {
                // Best effort cleanup on failure
            }

            throw;
        }
    }

    private async Task ResetSocketAsync()
    {
        if (_voiceSocket is not null)
        {
            try
            {
                await _voiceSocket.DisposeAsync();
            }
            catch
            {
                // Best effort cleanup
            }

            _voiceSocket = null;
        }
    }

    private bool HandleReplyActions(VoiceMessage reply)
    {
        var now = _timeProvider.GetUtcNow();
        var added = PendingConfirmations.AddFromReply(reply.Actions, now);
        if (added.Count > 0)
        {
            var active = PendingConfirmations.GetActive(now);
            for (var i = 0; i < active.Count; i++)
            {
                var action = active[i];
                if (added.Any(a => a.Id == action.Id))
                {
                    _output.WriteLine($"[CONFIRMATION REQUIRED] #{i + 1} ({action.Id}): {action.Description} [tool: {action.Tool}]");
                }
            }

            _output.WriteLine("[CONFIRMATION REQUIRED] Voice confirmations are disabled. Confirm on screen with '/approve <#|id>' or '/deny <#|id>'.");
            return true;
        }
        else if (reply.Awaiting == true && PendingConfirmations.HasPending(now))
        {
            _output.WriteLine("[CONFIRMATION REQUIRED] Pending actions still await screen confirmation ('/pending', '/approve', '/deny').");
            return true;
        }

        return false;
    }

    private void StartTrackedSpeech(string text, CancellationToken cancellationToken)
    {
        var scope = _echoSuppressor.BeginSpeaking(text);
        _indicator.SetStatus(ListeningStatus.SuppressingEcho);

        var speakTask = Task.Run(async () =>
        {
            try
            {
                await _macSystem.SayAsync(text, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelled on shutdown
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[Listening] Speech output failed: {ex.Message}");
            }
            finally
            {
                scope.Dispose();
                try
                {
                    await RefreshIndicatorAsync(CancellationToken.None);
                }
                catch
                {
                    // Best effort indicator refresh
                }
            }
        }, CancellationToken.None);

        lock (_stateGate)
        {
            _activeSpeechTask = speakTask;
        }
    }

    private void LogRejection(string? transcript, GateRejectionReason reason)
    {
        switch (reason)
        {
            case GateRejectionReason.VoiceConfirmationBlocked:
                _output.WriteLine($"[Listening] Blocked voice confirmation (\"{transcript}\"). Confirm or deny pending actions on screen ('/approve' or '/deny').");
                break;
            case GateRejectionReason.SessionLocked:
                _output.WriteLine("[Listening] Ignored speech while session is locked.");
                break;
            case GateRejectionReason.Muted:
                _output.WriteLine("[Listening] Ignored speech while microphone is muted.");
                break;
            case GateRejectionReason.EchoSuppressed:
                _output.WriteLine("[Listening] Suppressed self-voice echo.");
                break;
            case GateRejectionReason.NoCommandAfterWakeWord:
                _output.WriteLine("[Listening] Wake word heard without a command; nothing sent.");
                break;
            case GateRejectionReason.NotAddressedToMe:
                _output.WriteLine("[Listening] Ignored ambient speech (not addressed to JARVIS).");
                break;
        }
    }

    private static ListeningStatus ComputeStatus(bool isLocked, bool isMuted, bool isSuppressingEcho)
    {
        if (isLocked)
        {
            return ListeningStatus.Locked;
        }

        if (isMuted)
        {
            return ListeningStatus.Muted;
        }

        if (isSuppressingEcho)
        {
            return ListeningStatus.SuppressingEcho;
        }

        return ListeningStatus.Listening;
    }
}

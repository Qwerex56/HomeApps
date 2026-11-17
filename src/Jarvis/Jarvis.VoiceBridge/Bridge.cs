using System.Globalization;
using System.Net.WebSockets;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The bridge's loop (jarvis-voice-bridge.md 1): the wake word listens locally; after "Hey Jarvis" (or Enter) the
/// utterance streams to the server until the person is quiet; then the microphone is ignored while the server answers
/// and JARVIS speaks. When he asks "tak czy nie?", the next answer is recorded without the wake word.
/// </summary>
internal sealed class Bridge(BridgeOptions options, WakeWord wake, OAuthLogin login, VoiceLink link, Microphone microphone) : IDisposable
{
    private static readonly TimeSpan WaitAfterWake = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WaitForAnswer = TimeSpan.FromSeconds(8);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private BridgeState _state = BridgeState.Waiting;
    private EndOfSpeech _speech = new(WaitAfterWake);
    private List<short>? _recording;
    private bool _awaiting;
    private bool _resetWake;
    private CancellationTokenSource _playing = new();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var quit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ConnectAsync(quit.Token);
        Console.WriteLine($"Mikrofon: {microphone.Name}. Powiedz „Hey Jarvis” albo naciśnij Enter; q kończy.");
        var events = EventsAsync(quit.Token);
        var keys = Task.Run(() => KeysAsync(quit), quit.Token);
        try
        {
            await foreach (var frame in microphone.Frames.ReadAllAsync(quit.Token))
            {
                await OnFrameAsync(frame, quit.Token);
            }
        }
        catch (OperationCanceledException) when (quit.IsCancellationRequested)
        {
            // q or Ctrl+C.
        }

        await quit.CancelAsync();
        await Task.WhenAll(events, keys).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _gate.Dispose();
        _playing.Dispose();
    }

    private async Task OnFrameAsync(short[] frame, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            switch (_state)
            {
                case BridgeState.Waiting:
                    if (_resetWake)
                    {
                        wake.Reset();
                        _resetWake = false;
                    }

                    var score = wake.Process(frame);
                    if (score >= options.Threshold)
                    {
                        Console.WriteLine($"» Hey Jarvis ({score:0.00})");
                        await BeginAsync(BridgeState.Recording, cancellationToken);
                    }

                    break;
                case BridgeState.Recording or BridgeState.Answering:
                    await link.SendAudioAsync(frame, cancellationToken);
                    _recording?.AddRange(frame);
                    switch (_speech.Push(frame))
                    {
                        case SpeechState.Ended or SpeechState.TooLong:
                            await FinishAsync(cancellationToken);
                            break;
                        case SpeechState.NothingSaid:
                            await link.CancelAsync(cancellationToken);
                            Console.WriteLine(_state == BridgeState.Answering ? "(brak odpowiedzi, akcja dalej czeka na karcie)" : "(cisza)");
                            BackToWaiting();
                            break;
                    }

                    break;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Under the gate: the chime, then everything the microphone hears goes to the server.</summary>
    private async Task BeginAsync(BridgeState state, CancellationToken cancellationToken)
    {
        _state = state;
        _speech = new EndOfSpeech(state == BridgeState.Answering ? WaitForAnswer : WaitAfterWake);
        _recording = options.SaveRecordings is null ? null : [];
        await Speaker.PlayAsync(Chime.Listening, cancellationToken);
        microphone.Drain();
        await link.StartAsync(cancellationToken);
    }

    /// <summary>Under the gate: the utterance is complete, the server takes over.</summary>
    private async Task FinishAsync(CancellationToken cancellationToken)
    {
        await link.StopAsync(cancellationToken);
        _state = BridgeState.Busy;
        _ = Speaker.PlayAsync(Chime.Heard, cancellationToken);
        if (_recording is { Count: > 0 } recording && options.SaveRecordings is { } folder)
        {
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.wav");
            await File.WriteAllBytesAsync(file, Wav.Write(recording.ToArray(), Microphone.SampleRate), cancellationToken);
        }

        _recording = null;
    }

    private void BackToWaiting()
    {
        _state = BridgeState.Waiting;
        _awaiting = false;
        _resetWake = true;
    }

    private async Task EventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var said in link.Events.ReadAllAsync(cancellationToken))
            {
                await OnEventAsync(said, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Quitting.
        }
    }

    private async Task OnEventAsync(ServerEvent said, CancellationToken cancellationToken)
    {
        switch (said.Type)
        {
            case "transcript":
                var heard = said.Text("text");
                Console.WriteLine(string.IsNullOrWhiteSpace(heard) ? "Ty: (nic nie usłyszałem)" : $"Ty: {heard}");
                break;
            case "reply":
                Console.WriteLine($"JARVIS: {said.Text("text")}");
                if (said.Json.TryGetProperty("actions", out var actions))
                {
                    foreach (var action in actions.EnumerateArray())
                    {
                        Console.WriteLine($"  [{action.GetProperty("status").GetString()}] {action.GetProperty("description").GetString()}");
                    }
                }

                _awaiting = said.Flag("awaiting");
                break;
            case "audio" when said.Wav is { } wav:
                try
                {
                    await Speaker.PlayAsync(wav, _playing.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Enter stopped him.
                }

                break;
            case "done":
                await UnderGateAsync(async () =>
                {
                    if (_state != BridgeState.Busy)
                    {
                        return;
                    }

                    if (_awaiting && !said.Flag("cancelled"))
                    {
                        Console.WriteLine("(tak / nie?)");
                        await BeginAsync(BridgeState.Answering, cancellationToken);
                    }
                    else
                    {
                        BackToWaiting();
                    }
                }, cancellationToken);
                break;
            case "error":
                Console.WriteLine($"Błąd serwera: {said.Text("code")} {said.Text("detail")}".TrimEnd());
                await UnderGateAsync(() => { BackToWaiting(); return Task.CompletedTask; }, cancellationToken);
                break;
            case "closed" when !cancellationToken.IsCancellationRequested:
                Console.WriteLine("Połączenie z serwerem zerwane, łączę ponownie…");
                await UnderGateAsync(() => { BackToWaiting(); return Task.CompletedTask; }, cancellationToken);
                await ConnectAsync(cancellationToken);
                break;
        }
    }

    private void KeysAsync(CancellationTokenSource quit)
    {
        if (Console.IsInputRedirected)
        {
            return;
        }

        while (!quit.IsCancellationRequested)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q')
            {
                quit.Cancel();
                return;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                UnderGateAsync(() => OnEnterAsync(quit.Token), quit.Token).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>Enter: speak without the wake word, end the utterance now, or stop JARVIS mid-answer.</summary>
    private async Task OnEnterAsync(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BridgeState.Waiting:
                Console.WriteLine("» Enter");
                await BeginAsync(BridgeState.Recording, cancellationToken);
                break;
            case BridgeState.Recording or BridgeState.Answering:
                await FinishAsync(cancellationToken);
                break;
            case BridgeState.Busy:
                await _playing.CancelAsync();
                _playing.Dispose();
                _playing = new CancellationTokenSource();
                await link.CancelAsync(cancellationToken);
                Console.WriteLine("(przerwane)");
                BackToWaiting();
                break;
        }
    }

    private async Task UnderGateAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await link.ConnectAsync(await login.AccessTokenAsync(cancellationToken), cancellationToken);
                Console.WriteLine($"Połączono z {options.Server.Host}.");
                return;
            }
            catch (Exception exception) when (exception is HttpRequestException or WebSocketException or JsonException)
            {
                Console.WriteLine($"Nie mogę połączyć się z {options.Server.Host} ({exception.Message}); próbuję za 5 s.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}

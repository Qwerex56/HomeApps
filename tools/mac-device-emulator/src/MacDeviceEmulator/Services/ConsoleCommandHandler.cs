using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>
/// Interactive console command loop for H-8 controls (<c>/mute</c>, <c>/unmute</c>, <c>/lock</c>,
/// <c>/approve</c>, <c>/deny</c>, <c>/status</c>) and simulated local speech input lines.
/// </summary>
public sealed class ConsoleCommandHandler(
    ContinuousListeningService listeningService,
    ChannelSpeechInput speechInput,
    TimeProvider? timeProvider = null,
    TextWriter? output = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TextWriter _output = output ?? Console.Out;

    public async Task RunReaderAsync(TextReader input, CancellationToken cancellationToken)
    {
        PrintHelp();
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await input.ReadLineAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (line is null)
            {
                // Stdin EOF (e.g. non-interactive / redirected input): leave emulator running cleanly.
                break;
            }

            await HandleLineAsync(line, cancellationToken);
        }
    }

    public async Task HandleLineAsync(string line, CancellationToken cancellationToken = default)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (!trimmed.StartsWith('/'))
        {
            speechInput.TryWrite(trimmed);
            return;
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1].Trim() : null;

        switch (command)
        {
            case "/mute":
                listeningService.SetMuted(true);
                await listeningService.RefreshIndicatorAsync(cancellationToken);
                break;
            case "/unmute":
                listeningService.SetMuted(false);
                await listeningService.RefreshIndicatorAsync(cancellationToken);
                break;
            case "/toggle-mute" or "/m":
                listeningService.ToggleMute();
                await listeningService.RefreshIndicatorAsync(cancellationToken);
                break;
            case "/lock":
                listeningService.SetManualLock(true);
                await listeningService.RefreshIndicatorAsync(cancellationToken);
                break;
            case "/unlock":
                listeningService.SetManualLock(null);
                await listeningService.RefreshIndicatorAsync(cancellationToken);
                break;
            case "/approve":
                await HandleDecisionAsync(argument, approve: true, cancellationToken);
                break;
            case "/deny":
                await HandleDecisionAsync(argument, approve: false, cancellationToken);
                break;
            case "/pending":
                PrintPending();
                break;
            case "/status":
                await PrintStatusAsync(cancellationToken);
                break;
            case "/help":
                PrintHelp();
                break;
            default:
                _output.WriteLine($"[Console] Unknown command '{command}'. Type /help for available commands.");
                break;
        }
    }

    private async Task HandleDecisionAsync(string? targetToken, bool approve, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var target = listeningService.PendingConfirmations.ResolveTarget(targetToken, now);
        if (target is null)
        {
            _output.WriteLine("[Confirmation] No matching pending action to decide.");
            return;
        }

        await listeningService.DecideConfirmationOnScreenAsync(target.Id, approve, cancellationToken);
    }

    private void PrintPending()
    {
        var active = listeningService.PendingConfirmations.GetActive(_timeProvider.GetUtcNow());
        if (active.Count == 0)
        {
            _output.WriteLine("[Confirmation] No pending actions.");
            return;
        }

        for (var i = 0; i < active.Count; i++)
        {
            var action = active[i];
            _output.WriteLine($"  #{i + 1} {action.Id}: {action.Description} [tool: {action.Tool}] (expires: {action.ConfirmBy:u})");
        }
    }

    private async Task PrintStatusAsync(CancellationToken cancellationToken)
    {
        var status = await listeningService.RefreshIndicatorAsync(cancellationToken);
        var pending = listeningService.PendingConfirmations.GetActive(_timeProvider.GetUtcNow()).Count;
        _output.WriteLine(
            $"[Status] {ConsoleListeningIndicator.FormatBadge(status)} | Muted: {listeningService.IsMuted} | " +
            $"ManualLock: {listeningService.ManualLockOverride?.ToString() ?? "auto"} | " +
            $"Conversation: {listeningService.ConversationId?.ToString() ?? "none"} | Pending: {pending}");
    }

    private void PrintHelp()
    {
        _output.WriteLine(
            "[Listening] Interactive mode: type a phrase to simulate local speech (e.g. 'Jarvis, która godzina?'), " +
            "or commands: /mute, /unmute, /toggle-mute, /lock, /unlock, /approve [#|id], /deny [#|id], /pending, /status, /help");
    }
}

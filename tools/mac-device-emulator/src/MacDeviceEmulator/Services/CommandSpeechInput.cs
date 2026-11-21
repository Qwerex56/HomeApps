using System.Diagnostics;
using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>
/// Reads locally transcribed speech lines from a local STT child process (stdout, one utterance per line)
/// so audio never leaves the machine before the local gate decides.
/// </summary>
public sealed class CommandSpeechInput(
    string fileName,
    IReadOnlyList<string> arguments,
    TimeProvider? timeProvider = null) : ISpeechInput, IAsyncDisposable
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private Process? _process;
    private StreamReader? _stdout;

    public async Task<RecognizedSpeech?> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);
        if (_stdout is null)
        {
            return null;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _stdout.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                return new RecognizedSpeech(line.Trim(), _timeProvider.GetUtcNow());
            }
        }

        return null;
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_stdout is not null)
        {
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_stdout is not null)
            {
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            foreach (var arg in arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();
            _process = process;
            _stdout = process.StandardOutput;
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort process termination
            }

            _process.Dispose();
            _process = null;
        }

        _stdout?.Dispose();
        _stdout = null;
        _startLock.Dispose();
        await Task.CompletedTask;
    }
}

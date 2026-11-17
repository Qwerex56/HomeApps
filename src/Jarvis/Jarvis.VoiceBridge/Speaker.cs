using System.Diagnostics;

namespace Jarvis.VoiceBridge;

/// <summary>
/// Plays a WAV with the system's own player (<c>afplay</c> on macOS, <c>aplay</c> on Linux), so the bridge needs no
/// audio output code. Cancelling stops the sound at once.
/// </summary>
internal static class Speaker
{
    public static async Task PlayAsync(byte[] wav, CancellationToken cancellationToken)
    {
        var file = Path.Combine(Path.GetTempPath(), $"jarvis-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(file, wav, cancellationToken);
        var start = OperatingSystem.IsMacOS()
            ? new ProcessStartInfo("afplay") { ArgumentList = { file } }
            : new ProcessStartInfo("aplay") { ArgumentList = { "-q", file } };
        try
        {
            using var player = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {start.FileName}.");
            try
            {
                await player.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                player.Kill();
                throw;
            }
        }
        finally
        {
            File.Delete(file);
        }
    }
}

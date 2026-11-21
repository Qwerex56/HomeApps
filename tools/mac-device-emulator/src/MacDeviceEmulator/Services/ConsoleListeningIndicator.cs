using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>Displays the visible continuous listening indicator in the console.</summary>
public sealed class ConsoleListeningIndicator(TextWriter? output = null) : IListeningIndicator
{
    private readonly TextWriter _output = output ?? Console.Out;
    private readonly Lock _gate = new();
    private ListeningStatus _status = ListeningStatus.Stopped;
    private string? _lastDetail;

    public ListeningStatus CurrentStatus
    {
        get
        {
            lock (_gate)
            {
                return _status;
            }
        }
    }

    public void SetStatus(ListeningStatus status, string? detail = null)
    {
        lock (_gate)
        {
            if (_status == status && string.Equals(_lastDetail, detail, StringComparison.Ordinal))
            {
                return;
            }

            _status = status;
            _lastDetail = detail;
        }

        var badge = FormatBadge(status);
        var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" ({detail})";
        _output.WriteLine($"[Listening] {badge}{suffix}");
    }

    public static string FormatBadge(ListeningStatus status) => status switch
    {
        ListeningStatus.Listening => "[MIC: LISTENING]",
        ListeningStatus.Muted => "[MIC: MUTED]",
        ListeningStatus.Locked => "[MIC: PAUSED - SESSION LOCKED]",
        ListeningStatus.SuppressingEcho => "[MIC: PAUSED - JARVIS SPEAKING]",
        ListeningStatus.Stopped => "[MIC: OFF]",
        _ => "[MIC: UNKNOWN]"
    };
}

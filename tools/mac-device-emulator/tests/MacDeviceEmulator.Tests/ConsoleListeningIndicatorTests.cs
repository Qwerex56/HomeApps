using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class ConsoleListeningIndicatorTests
{
    [Theory]
    [InlineData(ListeningStatus.Listening, "[MIC: LISTENING]")]
    [InlineData(ListeningStatus.Muted, "[MIC: MUTED]")]
    [InlineData(ListeningStatus.Locked, "[MIC: PAUSED - SESSION LOCKED]")]
    [InlineData(ListeningStatus.SuppressingEcho, "[MIC: PAUSED - JARVIS SPEAKING]")]
    [InlineData(ListeningStatus.Stopped, "[MIC: OFF]")]
    [InlineData((ListeningStatus)999, "[MIC: UNKNOWN]")]
    public void FormatBadge_returns_expected_badge(ListeningStatus status, string expected)
    {
        Assert.Equal(expected, ConsoleListeningIndicator.FormatBadge(status));
    }

    [Fact]
    public void SetStatus_updates_status_and_writes_formatted_line()
    {
        using var sw = new StringWriter();
        var indicator = new ConsoleListeningIndicator(sw);

        Assert.Equal(ListeningStatus.Stopped, indicator.CurrentStatus);

        indicator.SetStatus(ListeningStatus.Listening);

        Assert.Equal(ListeningStatus.Listening, indicator.CurrentStatus);
        var output = sw.ToString().Trim();
        Assert.Equal("[Listening] [MIC: LISTENING]", output);
    }

    [Fact]
    public void SetStatus_includes_detail_when_provided()
    {
        using var sw = new StringWriter();
        var indicator = new ConsoleListeningIndicator(sw);

        indicator.SetStatus(ListeningStatus.Locked, "screen lock detected");

        var output = sw.ToString().Trim();
        Assert.Equal("[Listening] [MIC: PAUSED - SESSION LOCKED] (screen lock detected)", output);
    }

    [Fact]
    public void SetStatus_deduplicates_consecutive_identical_status_calls()
    {
        using var sw = new StringWriter();
        var indicator = new ConsoleListeningIndicator(sw);

        indicator.SetStatus(ListeningStatus.Listening);
        indicator.SetStatus(ListeningStatus.Listening);
        indicator.SetStatus(ListeningStatus.Listening);

        var lines = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }

    [Fact]
    public void SetStatus_emits_line_when_detail_changes()
    {
        using var sw = new StringWriter();
        var indicator = new ConsoleListeningIndicator(sw);

        indicator.SetStatus(ListeningStatus.Locked, "reason A");
        indicator.SetStatus(ListeningStatus.Locked, "reason B");

        var lines = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("reason A", lines[0]);
        Assert.Contains("reason B", lines[1]);
    }
}

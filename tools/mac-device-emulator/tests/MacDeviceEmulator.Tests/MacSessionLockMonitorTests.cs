using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Services;
using NSubstitute;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class MacSessionLockMonitorTests
{
    private readonly IProcessRunner _runner = Substitute.For<IProcessRunner>();
    private readonly MacSessionLockMonitor _monitor;

    public MacSessionLockMonitorTests()
    {
        _monitor = new MacSessionLockMonitor(_runner);
    }

    [Fact]
    public async Task Returns_true_when_CGSSessionScreenIsLocked_is_yes()
    {
        const string output = """
            +-o Root  <class IORegistryEntry>
              {
                "CGSSessionScreenIsLocked" = Yes
                "kCGSSessionOnConsoleKey" = Yes
              }
            """;

        _runner.RunAsync("ioreg", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, output, ""));

        var locked = await _monitor.IsLockedAsync();

        Assert.True(locked);
    }

    [Fact]
    public async Task Returns_true_when_off_console()
    {
        const string output = """
            +-o Root  <class IORegistryEntry>
              {
                "CGSSessionScreenIsLocked" = No
                "kCGSSessionOnConsoleKey" = No
              }
            """;

        _runner.RunAsync("ioreg", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, output, ""));

        var locked = await _monitor.IsLockedAsync();

        Assert.True(locked);
    }

    [Fact]
    public async Task Returns_false_when_unlocked_and_on_console()
    {
        const string output = """
            +-o Root  <class IORegistryEntry>
              {
                "CGSSessionScreenIsLocked" = No
                "kCGSSessionOnConsoleKey" = Yes
              }
            """;

        _runner.RunAsync("ioreg", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, output, ""));

        var locked = await _monitor.IsLockedAsync();

        Assert.False(locked);
    }

    [Fact]
    public async Task Returns_true_when_ioreg_fails_or_empty()
    {
        _runner.RunAsync("ioreg", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(1, "", "ioreg error"));

        var locked = await _monitor.IsLockedAsync();

        Assert.True(locked);
    }
}

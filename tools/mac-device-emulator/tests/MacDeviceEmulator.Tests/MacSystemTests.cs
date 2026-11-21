using System.Text.Json;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Services;
using NSubstitute;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class MacSystemTests
{
    private readonly IProcessRunner _runner = Substitute.For<IProcessRunner>();
    private readonly MacSystem _macSystem;

    public MacSystemTests()
    {
        _macSystem = new MacSystem(_runner);
    }

    [Fact]
    public async Task GetVolumeAsync_parses_single_line_volume()
    {
        _runner.RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("output volume of (get volume settings)")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, "42\n", ""));

        var vol = await _macSystem.GetVolumeAsync();

        Assert.Equal(42, vol);
    }

    [Fact]
    public async Task GetVolumeAsync_falls_back_to_full_settings_parsing()
    {
        _runner.RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("output volume of (get volume settings)")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(1, "", "error"));

        _runner.RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("get volume settings")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, "output volume:65, input volume:50, alert volume:70, output muted:false\n", ""));

        var vol = await _macSystem.GetVolumeAsync();

        Assert.Equal(65, vol);
    }

    [Fact]
    public async Task SetVolumeAsync_clamps_volume_between_0_and_100()
    {
        await _macSystem.SetVolumeAsync(150);
        await _macSystem.SetVolumeAsync(-10);

        await _runner.Received(1).RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("set volume output volume 100")), Arg.Any<CancellationToken>());
        await _runner.Received(1).RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("set volume output volume 0")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsMutedAsync_parses_muted_state()
    {
        _runner.RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("output muted of (get volume settings)")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, "true\n", ""));

        var muted = await _macSystem.IsMutedAsync();

        Assert.True(muted);
    }

    [Fact]
    public async Task SetMutedAsync_sets_mute_flag()
    {
        await _macSystem.SetMutedAsync(true);
        await _macSystem.SetMutedAsync(false);

        await _runner.Received(1).RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("set volume output muted true")), Arg.Any<CancellationToken>());
        await _runner.Received(1).RunAsync("osascript", Arg.Is<IReadOnlyList<string>>(args => args.Contains("set volume output muted false")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBatteryLevelAsync_parses_pmset_output()
    {
        var pmsetOutput = """
        Now drawing from 'Battery Power'
         -InternalBattery-0 (id=22544483)	78%; discharging; 4:12 remaining present: true
        """;

        _runner.RunAsync("pmset", Arg.Is<IReadOnlyList<string>>(args => args.Contains("batt")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, pmsetOutput, ""));

        var battery = await _macSystem.GetBatteryLevelAsync();

        Assert.Equal(78, battery);
    }

    [Fact]
    public async Task GetCpuLoadAsync_parses_top_output()
    {
        var topOutput = "CPU usage: 12.5% user, 8.2% sys, 79.3% idle\n";
        _runner.RunAsync("top", Arg.Is<IReadOnlyList<string>>(args => args.Contains("0")), Arg.Any<CancellationToken>())
            .Returns(new ProcessResult(0, topOutput, ""));

        var cpu = await _macSystem.GetCpuLoadAsync();

        Assert.Equal(20.7, cpu);
    }

    [Fact]
    public async Task SayAsync_passes_arguments_without_flag_injection()
    {
        await _macSystem.SayAsync("Hello World");
        await _macSystem.SayAsync("-v Alex Hello");

        await _runner.Received(1).RunAsync("say", Arg.Is<IReadOnlyList<string>>(args => args.SequenceEqual(new[] { "--", "Hello World" })), Arg.Any<CancellationToken>());
        await _runner.Received(1).RunAsync("say", Arg.Is<IReadOnlyList<string>>(args => args.SequenceEqual(new[] { "--", "-v Alex Hello" })), Arg.Any<CancellationToken>());
    }
}

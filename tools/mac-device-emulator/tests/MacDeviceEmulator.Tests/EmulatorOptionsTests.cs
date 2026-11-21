using MacDeviceEmulator.Configuration;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class EmulatorOptionsTests
{
    [Fact]
    public void Parses_continuous_listening_cli_arguments()
    {
        var args = new[]
        {
            "--no-listen",
            "--muted",
            "--stt-command", "/usr/local/bin/whisper",
            "--stt-arg", "--model",
            "--stt-arg", "base.en",
            "--wake-words", "jarvis,computer,hey siri"
        };

        var options = Program.ParseArguments(args);

        Assert.False(options.EnableContinuousListening);
        Assert.True(options.StartMuted);
        Assert.Equal("/usr/local/bin/whisper", options.SttCommand);
        Assert.Equal(["--model", "base.en"], options.SttArguments);
        Assert.NotNull(options.WakeWords);
        Assert.Equal(["jarvis", "computer", "hey siri"], options.WakeWords);
    }

    [Fact]
    public void Default_options_have_continuous_listening_enabled_and_unmuted()
    {
        var options = Program.ParseArguments([]);

        Assert.True(options.EnableContinuousListening);
        Assert.False(options.StartMuted);
        Assert.Null(options.SttCommand);
        Assert.Empty(options.SttArguments);
        Assert.Null(options.WakeWords);
    }
}

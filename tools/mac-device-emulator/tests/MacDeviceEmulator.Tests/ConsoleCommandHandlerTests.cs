using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Configuration;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;
using NSubstitute;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class ConsoleCommandHandlerTests
{
    private readonly ChannelSpeechInput _speechInput = new();
    private readonly IMacSystem _mac = Substitute.For<IMacSystem>();
    private readonly ISessionLockMonitor _lockMonitor = Substitute.For<ISessionLockMonitor>();
    private readonly ISpeechEchoSuppressor _echoSuppressor = Substitute.For<ISpeechEchoSuppressor>();
    private readonly IUtteranceGate _gate = Substitute.For<IUtteranceGate>();
    private readonly IListeningIndicator _indicator = Substitute.For<IListeningIndicator>();
    private readonly IHomeAppsClient _homeAppsClient = Substitute.For<IHomeAppsClient>();
    private readonly ContinuousListeningService _listeningService;
    private readonly ConsoleCommandHandler _handler;

    public ConsoleCommandHandlerTests()
    {
        _listeningService = new ContinuousListeningService(
            "http://localhost:5000",
            "token",
            () => Substitute.For<IVoiceWebSocket>(),
            _mac,
            _lockMonitor,
            _echoSuppressor,
            _gate,
            _indicator,
            _homeAppsClient);

        _handler = new ConsoleCommandHandler(_listeningService, _speechInput, output: TextWriter.Null);
    }

    [Fact]
    public async Task HandleLineAsync_passes_ambient_lines_to_speech_input()
    {
        await _handler.HandleLineAsync("Jarvis, włącz radio");

        var read = await _speechInput.ReadNextAsync();
        Assert.NotNull(read);
        Assert.Equal("Jarvis, włącz radio", read.Text);
    }

    [Fact]
    public async Task HandleLineAsync_handles_mute_and_unmute_commands()
    {
        Assert.False(_listeningService.IsMuted);

        await _handler.HandleLineAsync("/mute");
        Assert.True(_listeningService.IsMuted);

        await _handler.HandleLineAsync("/unmute");
        Assert.False(_listeningService.IsMuted);

        await _handler.HandleLineAsync("/toggle-mute");
        Assert.True(_listeningService.IsMuted);

        await _handler.HandleLineAsync("/m");
        Assert.False(_listeningService.IsMuted);
    }

    [Fact]
    public async Task HandleLineAsync_handles_lock_and_unlock_commands()
    {
        Assert.Null(_listeningService.ManualLockOverride);

        await _handler.HandleLineAsync("/lock");
        Assert.True(_listeningService.ManualLockOverride);

        await _handler.HandleLineAsync("/unlock");
        Assert.Null(_listeningService.ManualLockOverride);
    }

    [Fact]
    public async Task HandleLineAsync_handles_approve_and_deny_commands()
    {
        var actionId = Guid.NewGuid();
        _listeningService.PendingConfirmations.AddFromReply(
            [new VoiceActionView(actionId, "tool", "desc", "NeedsConfirmation", DateTimeOffset.UtcNow.AddMinutes(5))],
            DateTimeOffset.UtcNow);

        _homeAppsClient.DecideConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), actionId, true, Arg.Any<CancellationToken>())
            .Returns(FluentResults.Result.Ok(new VoiceActionView(actionId, "tool", "desc", "Approved")));

        await _handler.HandleLineAsync("/approve 1");

        await _homeAppsClient.Received(1).DecideConfirmationAsync(
            "http://localhost:5000",
            "token",
            actionId,
            true,
            Arg.Any<CancellationToken>());

        Assert.False(_listeningService.PendingConfirmations.HasPending(DateTimeOffset.UtcNow));
    }
}

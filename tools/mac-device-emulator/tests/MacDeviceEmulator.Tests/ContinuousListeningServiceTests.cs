using System.Net.WebSockets;
using FluentResults;
using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;
using NSubstitute;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class ContinuousListeningServiceTests
{
    private readonly IMacSystem _mac = Substitute.For<IMacSystem>();
    private readonly ISessionLockMonitor _lockMonitor = Substitute.For<ISessionLockMonitor>();
    private readonly ISpeechEchoSuppressor _echo = Substitute.For<ISpeechEchoSuppressor>();
    private readonly IUtteranceGate _gate = Substitute.For<IUtteranceGate>();
    private readonly IListeningIndicator _indicator = Substitute.For<IListeningIndicator>();
    private readonly IHomeAppsClient _homeAppsClient = Substitute.For<IHomeAppsClient>();
    private readonly IVoiceWebSocket _ws = Substitute.For<IVoiceWebSocket>();
    private readonly TestTimeProvider _time = new();

    private readonly ContinuousListeningService _service;

    public ContinuousListeningServiceTests()
    {
        _service = new ContinuousListeningService(
            "https://app.example.com",
            "user-token-123",
            () => _ws,
            _mac,
            _lockMonitor,
            _echo,
            _gate,
            _indicator,
            _homeAppsClient,
            _time,
            startMuted: false,
            output: TextWriter.Null);
    }

    [Theory]
    [InlineData("http://localhost:5000", "ws://localhost:5000/jarvis/voice?speech=local")]
    [InlineData("https://myhome.app/", "wss://myhome.app/jarvis/voice?speech=local")]
    public void BuildVoiceUri_builds_correct_websocket_uri(string serverUrl, string expectedUri)
    {
        var uri = ContinuousListeningService.BuildVoiceUri(serverUrl);
        Assert.Equal(expectedUri, uri.ToString());
    }

    [Fact]
    public async Task ProcessUtteranceAsync_when_rejected_by_gate_does_not_open_websocket()
    {
        _gate.Evaluate(Arg.Any<string>(), Arg.Any<GateContext>())
            .Returns(new GateDecision(false, null, GateRejectionReason.NotAddressedToMe));

        var decision = await _service.ProcessUtteranceAsync("Ambient room noise");

        Assert.False(decision.Accepted);
        await _ws.DidNotReceiveWithAnyArgs().ConnectAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessUtteranceAsync_when_accepted_sends_to_voice_channel_and_speaks_reply()
    {
        _gate.Evaluate("Jarvis, która godzina?", Arg.Any<GateContext>())
            .Returns(new GateDecision(true, "która godzina?", GateRejectionReason.None));

        _ws.State.Returns(WebSocketState.None, WebSocketState.Open);

        var convId = Guid.NewGuid();
        var incomingMessages = new Queue<VoiceMessage?>([
            new VoiceMessage("ready", ConversationId: convId),
            new VoiceMessage("transcript", Text: "która godzina?"),
            new VoiceMessage("reply", Text: "Jest godzina dwunasta.", ConversationId: convId),
            new VoiceMessage("done")
        ]);

        _ws.ReceiveMessageAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(incomingMessages.Count > 0 ? incomingMessages.Dequeue() : null));

        var decision = await _service.ProcessUtteranceAsync("Jarvis, która godzina?");

        Assert.True(decision.Accepted);

        // Verify WebSocket handshake & turn
        await _ws.Received(1).ConnectAsync(
            new Uri("wss://app.example.com/jarvis/voice?speech=local"),
            "user-token-123",
            Arg.Any<CancellationToken>());

        await _ws.Received(1).SendMessageAsync(
            Arg.Is<VoiceMessage>(m => m.Type == "hello"),
            Arg.Any<CancellationToken>());

        await _ws.Received(1).SendMessageAsync(
            Arg.Is<VoiceMessage>(m => m.Type == "say" && m.Text == "która godzina?"),
            Arg.Any<CancellationToken>());

        // Wait for background speech task
        await _service.WaitForSpeechAsync();

        // Verify speech synthesis occurred
        await _mac.Received(1).SayAsync("Jest godzina dwunasta.", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessUtteranceAsync_stores_actions_awaiting_screen_confirmation()
    {
        _gate.Evaluate("Jarvis, wyłącz zasilanie", Arg.Any<GateContext>())
            .Returns(new GateDecision(true, "wyłącz zasilanie", GateRejectionReason.None));

        _ws.State.Returns(WebSocketState.None, WebSocketState.Open);

        var actionId = Guid.NewGuid();
        var action = new VoiceActionView(actionId, "power.off", "Wyłącz zasilanie urządzeń", "NeedsConfirmation", _time.GetUtcNow().AddMinutes(5));

        var incomingMessages = new Queue<VoiceMessage?>([
            new VoiceMessage("ready"),
            new VoiceMessage("reply", Text: "Czy na pewno wyłączyć zasilanie?", Actions: [action], Awaiting: true),
            new VoiceMessage("done")
        ]);

        _ws.ReceiveMessageAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(incomingMessages.Count > 0 ? incomingMessages.Dequeue() : null));

        await _service.ProcessUtteranceAsync("Jarvis, wyłącz zasilanie");

        Assert.True(_service.PendingConfirmations.HasPending(_time.GetUtcNow()));
        var pending = _service.PendingConfirmations.GetActive(_time.GetUtcNow());
        Assert.Single(pending);
        Assert.Equal(actionId, pending[0].Id);
    }

    [Fact]
    public async Task DecideConfirmationOnScreenAsync_calls_client_and_removes_from_store()
    {
        var actionId = Guid.NewGuid();
        _service.PendingConfirmations.AddFromReply(
            [new VoiceActionView(actionId, "power.off", "Wyłącz", "NeedsConfirmation", _time.GetUtcNow().AddMinutes(5))],
            _time.GetUtcNow());

        _homeAppsClient.DecideConfirmationAsync(
            "https://app.example.com",
            "user-token-123",
            actionId,
            true,
            Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new VoiceActionView(actionId, "power.off", "Wyłącz", "Approved")));

        var result = await _service.DecideConfirmationOnScreenAsync(actionId, true);

        Assert.True(result.IsSuccess);
        Assert.False(_service.PendingConfirmations.HasPending(_time.GetUtcNow()));
    }
}

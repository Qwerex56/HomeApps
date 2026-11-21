using MacDeviceEmulator.Abstractions;
using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class LocalUtteranceGateTests
{
    private readonly LocalUtteranceGate _gate = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    [InlineData(", , ,")]
    public void Rejects_empty_or_whitespace_or_punctuation_only(string? input)
    {
        var decision = _gate.Evaluate(input, new GateContext(false, false, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.Empty, decision.Reason);
        Assert.Null(decision.CommandText);
    }

    [Fact]
    public void Rejects_when_session_is_locked()
    {
        var decision = _gate.Evaluate("Jarvis, włącz światło", new GateContext(false, true, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.SessionLocked, decision.Reason);
    }

    [Fact]
    public void Rejects_when_muted()
    {
        var decision = _gate.Evaluate("Jarvis, włącz światło", new GateContext(true, false, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.Muted, decision.Reason);
    }

    [Fact]
    public void Rejects_when_echo_is_detected()
    {
        var decision = _gate.Evaluate("Jarvis, włącz światło", new GateContext(false, false, true, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.EchoSuppressed, decision.Reason);
    }

    [Theory]
    [InlineData("Co dzisiaj jemy na obiad?")]
    [InlineData("W pokoju obok jest dość głośno")]
    [InlineData("Ktoś dzwonił do drzwi")]
    [InlineData("Hello everyone in the meeting")]
    public void Rejects_speech_not_addressed_to_jarvis(string speech)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.NotAddressedToMe, decision.Reason);
    }

    [Theory]
    [InlineData("Jarvis")]
    [InlineData("jarvis")]
    [InlineData("Hej Jarvis")]
    [InlineData("hej jarvis!")]
    [InlineData("ok jarvis...")]
    [InlineData("słuchaj jarvisie:")]
    public void Rejects_wake_word_alone_without_command(string wakeOnly)
    {
        var decision = _gate.Evaluate(wakeOnly, new GateContext(false, false, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.NoCommandAfterWakeWord, decision.Reason);
    }

    [Theory]
    [InlineData("Jarvis, włącz światło", "włącz światło")]
    [InlineData("Hej Jarvis: która godzina?", "która godzina?")]
    [InlineData("Hey Jarvis turn off kitchen light", "turn off kitchen light")]
    [InlineData("ok jarvis, zgaś lampkę", "zgaś lampkę")]
    [InlineData("cześć jarvis, co słychać?", "co słychać?")]
    [InlineData("słuchaj jarvisie: jak pogoda", "jak pogoda")]
    public void Accepts_prefix_wake_word_and_extracts_command(string speech, string expectedCommand)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, false));

        Assert.True(decision.Accepted);
        Assert.Equal(GateRejectionReason.None, decision.Reason);
        Assert.Equal(expectedCommand, decision.CommandText);
    }

    [Theory]
    [InlineData("Włącz światło w salonie, Jarvis", "Włącz światło w salonie")]
    [InlineData("Która jest godzina, Jarvis?", "Która jest godzina")]
    [InlineData("Zagraj ulubioną playlistę, hej jarvis!", "Zagraj ulubioną playlistę")]
    public void Accepts_suffix_vocative_wake_word_and_extracts_command(string speech, string expectedCommand)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, false));

        Assert.True(decision.Accepted);
        Assert.Equal(GateRejectionReason.None, decision.Reason);
        Assert.Equal(expectedCommand, decision.CommandText);
    }

    [Theory]
    [InlineData("Rozmawiałem wczoraj o Jarvis")]
    [InlineData("To jest prezent dla Jarvis")]
    [InlineData("Wróciłem do domu z Jarvis")]
    [InlineData("Paczka przyszła od Jarvis")]
    public void Rejects_third_person_prepositional_mentions(string speech)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, false));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.NotAddressedToMe, decision.Reason);
    }

    [Theory]
    [InlineData("Jarvis, tak")]
    [InlineData("Hej Jarvis: potwierdzam")]
    [InlineData("Jarvis, zgoda")]
    [InlineData("Jarvis, zrób to")]
    [InlineData("Jarvis, nie")]
    [InlineData("Jarvis, anuluj")]
    [InlineData("Hey Jarvis, approve")]
    [InlineData("Hey Jarvis, cancel")]
    public void Blocks_voice_consent_when_confirmations_are_pending(string speech)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, HasPendingConfirmations: true));

        Assert.False(decision.Accepted);
        Assert.Equal(GateRejectionReason.VoiceConfirmationBlocked, decision.Reason);
        Assert.Null(decision.CommandText);
    }

    [Theory]
    [InlineData("Jarvis, włącz radio", "włącz radio")]
    [InlineData("Hej Jarvis: która godzina?", "która godzina?")]
    [InlineData("Jarvis, jaka jest pogoda?", "jaka jest pogoda?")]
    public void Allows_unrelated_commands_when_confirmations_are_pending(string speech, string expectedCommand)
    {
        var decision = _gate.Evaluate(speech, new GateContext(false, false, false, HasPendingConfirmations: true));

        Assert.True(decision.Accepted);
        Assert.Equal(GateRejectionReason.None, decision.Reason);
        Assert.Equal(expectedCommand, decision.CommandText);
    }

    [Fact]
    public void Allows_custom_wake_words()
    {
        var customGate = new LocalUtteranceGate(["komputer", "maczek"]);

        var ignored = customGate.Evaluate("Jarvis, włącz muzykę", new GateContext(false, false, false, false));
        Assert.False(ignored.Accepted);
        Assert.Equal(GateRejectionReason.NotAddressedToMe, ignored.Reason);

        var accepted = customGate.Evaluate("Komputer, zagraj radio", new GateContext(false, false, false, false));
        Assert.True(accepted.Accepted);
        Assert.Equal("zagraj radio", accepted.CommandText);
    }
}

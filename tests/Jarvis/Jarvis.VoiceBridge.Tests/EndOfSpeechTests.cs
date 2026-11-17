namespace Jarvis.VoiceBridge.Tests;

/// <summary>When the bridge stops sending: after a short quiet once something was said, never in the middle.</summary>
public sealed class EndOfSpeechTests
{
    [Fact]
    public void Speech_then_quiet_ends_the_utterance_after_the_silence()
    {
        var speech = new EndOfSpeech(TimeSpan.FromSeconds(5));
        var states = Frames.For(TimeSpan.FromSeconds(1), Frames.Speech)
            .Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet))
            .Select(speech.Push)
            .ToList();

        var ended = states.IndexOf(SpeechState.Ended);
        Assert.True(speech.Heard);
        Assert.InRange(ended * 80, 1000 + 640, 1000 + 800);
    }

    [Fact]
    public void A_short_pause_between_words_does_not_end_it()
    {
        var speech = new EndOfSpeech(TimeSpan.FromSeconds(5));
        var states = Frames.For(TimeSpan.FromMilliseconds(500), Frames.Speech)
            .Concat(Frames.For(TimeSpan.FromMilliseconds(400), Frames.Quiet))
            .Concat(Frames.For(TimeSpan.FromMilliseconds(500), Frames.Speech))
            .Select(speech.Push);

        Assert.All(states, s => Assert.Equal(SpeechState.Going, s));
    }

    [Fact]
    public void Nothing_said_after_the_chime_gives_up_after_the_wait()
    {
        var speech = new EndOfSpeech(TimeSpan.FromSeconds(2));
        var states = Frames.For(TimeSpan.FromSeconds(3), Frames.Quiet).Select(speech.Push).ToList();

        Assert.False(speech.Heard);
        Assert.Equal((2000 / 80) - 1, states.IndexOf(SpeechState.NothingSaid));
    }

    [Fact]
    public void Talking_on_and_on_stops_at_the_longest_utterance()
    {
        var speech = new EndOfSpeech(TimeSpan.FromSeconds(5));
        var states = Frames.For(EndOfSpeech.Longest + TimeSpan.FromSeconds(1), Frames.Speech).Select(speech.Push).ToList();

        Assert.Contains(SpeechState.TooLong, states);
        Assert.DoesNotContain(SpeechState.Ended, states);
    }
}

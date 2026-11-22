using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class SpeechEchoSuppressorTests
{
    [Fact]
    public void IsSuppressing_true_during_speaking_scope()
    {
        var time = new TestTimeProvider();
        var suppressor = new SpeechEchoSuppressor(time, cooldown: TimeSpan.FromSeconds(2));

        Assert.False(suppressor.IsSuppressing);

        using (suppressor.BeginSpeaking("Dzień dobry, dzisiaj będzie słonecznie."))
        {
            Assert.True(suppressor.IsSuppressing);
            Assert.True(suppressor.IsEcho("Dzień dobry"));
        }

        // Within cooldown
        Assert.True(suppressor.IsSuppressing);
        Assert.True(suppressor.IsEcho("coś innego"));

        // After cooldown expires
        time.Advance(TimeSpan.FromSeconds(2.1));
        Assert.False(suppressor.IsSuppressing);
    }

    [Fact]
    public void Detects_exact_or_substring_echo_within_memory_window()
    {
        var time = new TestTimeProvider();
        var suppressor = new SpeechEchoSuppressor(time, cooldown: TimeSpan.FromSeconds(2), memoryWindow: TimeSpan.FromSeconds(10));

        using (suppressor.BeginSpeaking("Temperatura w salonie wynosi dwadzieścia dwa stopnie."))
        {
        }

        time.Advance(TimeSpan.FromSeconds(2.5)); // cooldown passed
        Assert.False(suppressor.IsSuppressing);

        // Substring from spoken text heard shortly after
        Assert.True(suppressor.IsEcho("dwadzieścia dwa stopnie"));
        Assert.True(suppressor.IsEcho("Temperatura w salonie"));

        // Unrelated speech is not echo
        Assert.False(suppressor.IsEcho("włącz światło w kuchni"));
    }

    [Fact]
    public void Detects_echo_by_word_overlap_ratio()
    {
        var time = new TestTimeProvider();
        var suppressor = new SpeechEchoSuppressor(time, cooldown: TimeSpan.FromSeconds(2), memoryWindow: TimeSpan.FromSeconds(10));

        using (suppressor.BeginSpeaking("Włączam odtwarzanie muzyki w sypialni."))
        {
        }

        time.Advance(TimeSpan.FromSeconds(2.5));

        // 3 of 4 words match (75% >= 60%)
        Assert.True(suppressor.IsEcho("Włączam muzyki w sypialni"));

        // < 60% match
        Assert.False(suppressor.IsEcho("wyłączam ekspres do kawy"));
    }

    [Fact]
    public void Echo_memory_window_expires()
    {
        var time = new TestTimeProvider();
        var suppressor = new SpeechEchoSuppressor(time, cooldown: TimeSpan.FromSeconds(2), memoryWindow: TimeSpan.FromSeconds(10));

        using (suppressor.BeginSpeaking("Światło w kuchni zostało zgaszone."))
        {
        }

        time.Advance(TimeSpan.FromSeconds(11));

        Assert.False(suppressor.IsSuppressing);
        Assert.False(suppressor.IsEcho("Światło w kuchni zostało zgaszone"));
    }

    [Fact]
    public async Task TrackSpeakingAsync_wraps_action_and_suppresses_speech()
    {
        var time = new TestTimeProvider();
        var suppressor = new SpeechEchoSuppressor(time, cooldown: TimeSpan.FromSeconds(2));
        var executed = false;

        await suppressor.TrackSpeakingAsync("Test text", async _ =>
        {
            await Task.Yield();
            Assert.True(suppressor.IsSuppressing);
            executed = true;
        });

        Assert.True(executed);
        Assert.True(suppressor.IsSuppressing); // in cooldown
    }
}

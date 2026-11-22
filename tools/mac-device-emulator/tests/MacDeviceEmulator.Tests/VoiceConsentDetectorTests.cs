using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class VoiceConsentDetectorTests
{
    [Theory]
    [InlineData("tak")]
    [InlineData("nie")]
    [InlineData("potwierdzam")]
    [InlineData("potwierdź")]
    [InlineData("zgoda")]
    [InlineData("jasne")]
    [InlineData("ok")]
    [InlineData("okej")]
    [InlineData("zrób to")]
    [InlineData("anuluj")]
    [InlineData("odwołaj")]
    [InlineData("stop")]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    [InlineData("approve")]
    [InlineData("deny")]
    [InlineData("tak proszę")]
    [InlineData("zrób to proszę")]
    public void Returns_true_for_consent_or_rejection_phrases(string phrase)
    {
        Assert.True(VoiceConsentDetector.IsConsentPhrase(phrase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("włącz światło w salonie")]
    [InlineData("jaka jest dzisiaj pogoda")]
    [InlineData("ustaw budzik na siódmą rano")]
    [InlineData("to nie jest dobre rozwiązanie ponieważ tak uważam")]
    public void Returns_false_for_non_consent_utterances(string? phrase)
    {
        Assert.False(VoiceConsentDetector.IsConsentPhrase(phrase!));
    }
}

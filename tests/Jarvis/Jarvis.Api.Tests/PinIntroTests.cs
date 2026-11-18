using Jarvis.Application.Voice;

namespace Jarvis.Api.Tests;

/// <summary>jarvis-satellites.md S4: "tu Adrian, PIN 1234, …" as speech-to-text writes it down.</summary>
public sealed class PinIntroTests
{
    [Theory]
    [InlineData("Tu Adrian, PIN 1234, włącz światło w salonie.", "Adrian", "1234", "włącz światło w salonie")]
    [InlineData("mówi Ewa pin jeden dwa trzy cztery", "Ewa", "1234", "")]
    [InlineData("Jarvis, PIN: 98-76-54. Co mam dziś w planie?", null, "987654", "Jarvis Co mam dziś w planie?")]
    [InlineData("dodaj chleb, tu Ola PIN 5 5 5 5", "Ola", "5555", "dodaj chleb")]
    public void The_name_the_PIN_and_the_request_are_told_apart(string heard, string? name, string pin, string request)
    {
        var intro = PinIntro.Of(heard)!;

        Assert.Equal(name, intro.Name);
        Assert.Equal(pin, intro.Pin);
        Assert.Equal(request, intro.Request);

        Assert.DoesNotContain(pin[..4], intro.Masked, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("dodaj mleko do zakupów")]
    [InlineData("PIN 12, włącz światło")]
    [InlineData("przypnij notatkę 1234")]
    public void Without_four_digits_after_PIN_it_is_an_ordinary_request(string heard) =>
        Assert.Null(PinIntro.Of(heard));

    [Theory]
    [InlineData("lukasz", "Łukasz Nowak", true)]
    [InlineData("ADRIAN", "Adrian", true)]
    [InlineData("Ewa", "Adrian", false)]
    [InlineData(null, "Adrian", false)]
    public void Names_match_without_case_or_Polish_letters(string? spoken, string name, bool same) =>
        Assert.Equal(same, PinIntro.SameName(spoken, name));
}

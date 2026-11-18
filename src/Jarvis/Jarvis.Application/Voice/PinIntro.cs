using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jarvis.Application.Voice;

/// <summary>
/// "Tu Adrian, PIN 1234, włącz światło" heard by a household satellite (jarvis-satellites.md S4): who speaks, the PIN
/// (digits or Polish digit words) and the request. The PIN goes only to Auth: never to the model, a log or the
/// listener, which gets <see cref="Masked"/> back as the transcript.
/// </summary>
/// <param name="Name">The name before the PIN ("tu Adrian", "mówi Ewa"), to narrow who it may be; <c>null</c> when none.</param>
/// <param name="Request">What was asked besides the introduction; empty for "tu Adrian, PIN 1234" alone.</param>
public sealed partial record PinIntro(string? Name, string Pin, string Request, string Masked)
{
    private const int MinDigits = 4;
    private const int MaxDigits = 8;

    private static readonly Dictionary<string, char> DigitWords = new(StringComparer.Ordinal)
    {
        ["zero"] = '0', ["jeden"] = '1', ["jedna"] = '1', ["jedynka"] = '1', ["dwa"] = '2', ["dwójka"] = '2',
        ["trzy"] = '3', ["trójka"] = '3', ["cztery"] = '4', ["czwórka"] = '4', ["pięć"] = '5', ["piątka"] = '5',
        ["sześć"] = '6', ["szóstka"] = '6', ["siedem"] = '7', ["siódemka"] = '7', ["osiem"] = '8', ["ósemka"] = '8',
        ["dziewięć"] = '9', ["dziewiątka"] = '9',
    };

    /// <returns><c>null</c> when no PIN of four to eight digits follows the word "PIN".</returns>
    public static PinIntro? Of(string transcript)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        var word = PinWord().Match(transcript);
        if (!word.Success)
        {
            return null;
        }

        var pin = new StringBuilder();
        var end = word.Index + word.Length;
        var i = end;
        while (pin.Length < MaxDigits && i < transcript.Length)
        {
            while (i < transcript.Length && transcript[i] is ' ' or ',' or '-' or '.' or ':')
            {
                i++;
            }

            if (i < transcript.Length && char.IsAsciiDigit(transcript[i]))
            {
                pin.Append(transcript[i++]);
            }
            else if (Letters().Match(transcript, i) is { Success: true } letters && letters.Index == i
                && DigitWords.TryGetValue(letters.Value.ToLowerInvariant(), out var digit))
            {
                pin.Append(digit);
                i += letters.Length;
            }
            else
            {
                break;
            }

            end = i;
        }

        if (pin.Length < MinDigits)
        {
            return null;
        }

        var before = transcript[..word.Index];
        var after = transcript[end..];
        string? name = null;
        if (NameIntro().Matches(before) is { Count: > 0 } intros)
        {
            var intro = intros[^1];
            name = intro.Groups["name"].Value;
            before = before.Remove(intro.Index, intro.Length);
        }

        var request = Tidy($"{Tidy(before)} {Tidy(after)}");
        var masked = $"{transcript[..word.Index]}PIN ••••{transcript[end..]}";
        return new PinIntro(name, pin.ToString(), request, masked);
    }

    /// <summary>The same name ignoring case and Polish letters: "Łukasz" and "lukasz".</summary>
    public static bool SameName(string? spoken, string? name)
    {
        if (string.IsNullOrWhiteSpace(spoken) || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var first = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return string.Equals(Plain(spoken), Plain(first), StringComparison.Ordinal);
    }

    private static string Plain(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace('ł', 'l').Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    private static string Tidy(string text) => text.Trim(' ', ',', '.', ':', ';', '-', '!');

    [GeneratedRegex(@"\bpin(?:u|em)?\b[\s:,.-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PinWord();

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Letters();

    [GeneratedRegex(@"\b(?:tu|mówi|to|jestem)\s+(?<name>\p{L}+)[\s,]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NameIntro();
}

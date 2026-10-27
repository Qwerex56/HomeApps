using System.Text.RegularExpressions;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>
/// The UI and JARVIS language as a BCP 47 tag: language, optional script and region (<c>pl</c>, <c>pl-PL</c>,
/// <c>sr-Latn-RS</c>). Checked by shape, not against the machine's culture data, so it behaves the same everywhere.
/// </summary>
public sealed partial record Locale
{
    public static readonly Locale Default = new("pl-PL");

    private Locale(string value) => Value = value;

    public string Value { get; }

    public static Result<Locale> Create(string? raw)
    {
        var match = Pattern().Match(raw?.Trim() ?? string.Empty);
        if (!match.Success)
        {
            return Result.Fail<Locale>(UserErrors.LocaleInvalid);
        }

        var value = match.Groups["lang"].Value.ToLowerInvariant();
        if (match.Groups["script"].Success)
        {
            var script = match.Groups["script"].Value;
            value += "-" + char.ToUpperInvariant(script[0]) + script[1..].ToLowerInvariant();
        }

        if (match.Groups["region"].Success)
        {
            value += "-" + match.Groups["region"].Value.ToUpperInvariant();
        }

        return new Locale(value);
    }

    /// <summary>
    /// Q3: the browser's language from the registration context, or <see cref="Default"/>. Takes the first entry of an
    /// Accept-Language style list (<c>pl-PL,pl;q=0.9</c>).
    /// </summary>
    public static Locale FromRegistration(string? raw)
    {
        var first = raw?.Split(',', ';')[0];
        var result = Create(first);
        return result.IsSuccess ? result.Value : Default;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^(?<lang>[A-Za-z]{2,3})(?:[-_](?<script>[A-Za-z]{4}))?(?:[-_](?<region>[A-Za-z]{2}|\d{3}))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}

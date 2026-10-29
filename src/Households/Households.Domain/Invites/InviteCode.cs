using System.Security.Cryptography;
using FluentResults;

namespace Households.Domain.Invites;

/// <summary>
/// Q1: a 10-character code from the Crockford base32 alphabet (about 50 random bits), shown as <c>ABCDE-FGHJK</c>.
/// Case and dashes do not matter; <c>O</c> reads as <c>0</c> and <c>I</c>/<c>L</c> as <c>1</c>, so a code read aloud still works.
/// Stored as is, so the link can be copied again from the list. Used by invites (D3) and the guest link (D2b).
/// </summary>
public sealed record InviteCode
{
    public const int Length = 10;

    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private InviteCode(string value) => Value = value;

    /// <summary>The ten characters without the dash.</summary>
    public string Value { get; }

    /// <summary>The form people see and type: <c>ABCDE-FGHJK</c>.</summary>
    public string Display => $"{Value[..5]}-{Value[5..]}";

    public static InviteCode Generate() => new(new string(RandomNumberGenerator.GetItems<char>(Alphabet, Length)));

    /// <summary>Anything that cannot be a code is "not found", so a wrong guess learns nothing (Q3).</summary>
    public static Result<InviteCode> Parse(string? raw)
    {
        if (raw is null)
        {
            return Result.Fail<InviteCode>(InviteErrors.NotFound);
        }

        var chars = raw.Where(c => c is not ('-' or ' ')).Select(Normalize).ToArray();
        if (chars.Length != Length || chars.Any(c => !Alphabet.Contains(c, StringComparison.Ordinal)))
        {
            return Result.Fail<InviteCode>(InviteErrors.NotFound);
        }

        return new InviteCode(new string(chars));
    }

    public override string ToString() => Display;

    private static char Normalize(char c) => char.ToUpperInvariant(c) switch
    {
        'O' => '0',
        'I' or 'L' => '1',
        var upper => upper,
    };
}

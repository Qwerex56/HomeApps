using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentResults;

namespace Auth.Domain.Invitations;

/// <summary>
/// Short readable invitation id such as <c>K7P-2QX</c>, stored with the registration as the "reflink".
/// 32 characters without 0/O/1/I, six of them: about a billion codes; a unique index catches the rare clash (P6).
/// </summary>
public sealed partial record InvitationCode
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private InvitationCode(string value) => Value = value;

    public string Value { get; }

    public static InvitationCode New()
    {
        var chars = RandomNumberGenerator.GetItems<char>(Alphabet, 6);
        return new InvitationCode($"{new string(chars, 0, 3)}-{new string(chars, 3, 3)}");
    }

    /// <summary>A code typed in or read back from storage. Case and surrounding spaces do not matter.</summary>
    public static Result<InvitationCode> Create(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return Pattern().IsMatch(normalized)
            ? new InvitationCode(normalized)
            : Result.Fail<InvitationCode>(InvitationErrors.CodeInvalid);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[2-9A-HJ-NP-Z]{3}-[2-9A-HJ-NP-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}

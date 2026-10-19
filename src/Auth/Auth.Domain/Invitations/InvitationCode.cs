using System.Security.Cryptography;

namespace Auth.Domain.Invitations;

/// <summary>
/// Short readable invitation id such as <c>K7P-2QX</c>, stored with the registration as the "reflink".
/// 32 characters without 0/O/1/I, six of them: about a billion codes; a unique index catches the rare clash (P6).
/// </summary>
public sealed record InvitationCode
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private InvitationCode(string value) => Value = value;

    public string Value { get; }

    public static InvitationCode New()
    {
        var chars = RandomNumberGenerator.GetItems<char>(Alphabet, 6);
        return new InvitationCode($"{new string(chars, 0, 3)}-{new string(chars, 3, 3)}");
    }

    public override string ToString() => Value;
}

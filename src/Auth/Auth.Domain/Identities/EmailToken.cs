using Auth.Domain.Common;

namespace Auth.Domain.Identities;

/// <summary>
/// The one pending change an identity may have, confirmed by a link sent by email (R9). Until the link is used
/// nothing changes; a newer request of any kind replaces the pending one. One hour, single use, stored as a hash.
/// </summary>
public sealed record EmailToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private EmailToken(string tokenHash, EmailTokenPurpose purpose, DateTimeOffset expiresAt, Email? newEmail, string? newPasswordHash)
    {
        TokenHash = tokenHash;
        Purpose = purpose;
        ExpiresAt = expiresAt;
        NewEmail = newEmail;
        NewPasswordHash = newPasswordHash;
    }

    public string TokenHash { get; }

    public EmailTokenPurpose Purpose { get; }

    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Set only for <see cref="EmailTokenPurpose.EmailChange"/>.</summary>
    public Email? NewEmail { get; }

    /// <summary>Set only for <see cref="EmailTokenPurpose.PasswordChange"/>: the new password waits here, hashed.</summary>
    public string? NewPasswordHash { get; }

    internal static (EmailToken Token, string Raw) ForPasswordReset(DateTimeOffset now) =>
        Issue(EmailTokenPurpose.PasswordReset, now, null, null);

    internal static (EmailToken Token, string Raw) ForPasswordChange(string newPasswordHash, DateTimeOffset now) =>
        Issue(EmailTokenPurpose.PasswordChange, now, null, newPasswordHash);

    internal static (EmailToken Token, string Raw) ForEmailChange(Email newEmail, DateTimeOffset now) =>
        Issue(EmailTokenPurpose.EmailChange, now, newEmail, null);

    public bool Matches(string rawToken, EmailTokenPurpose purpose, DateTimeOffset now) =>
        Purpose == purpose && ExpiresAt > now && TokenHash == OpaqueToken.HashOf(rawToken);

    private static (EmailToken, string) Issue(EmailTokenPurpose purpose, DateTimeOffset now, Email? newEmail, string? newPasswordHash)
    {
        var (raw, hash) = OpaqueToken.New();
        return (new EmailToken(hash, purpose, now + Lifetime, newEmail, newPasswordHash), raw);
    }
}

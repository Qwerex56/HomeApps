using Accounts.Domain.Common;

namespace Accounts.Domain.Users;

/// <summary>
/// How and where the account was created (accounts.md 3.3). Written once, never changed (R6). Copied from Auth's
/// <c>IdentityRegistered</c>; free text is cleaned and cut instead of failing. No IP address (D6).
/// </summary>
public sealed record Registration
{
    public const int ClientAppMaxLength = 64;
    public const int UserAgentMaxLength = 512;
    public const int LocaleMaxLength = 35;

    private Registration(
        RegistrationSource source,
        DateTimeOffset registeredAt,
        bool viaReferral,
        string? invitedByEmail,
        string? invitedEmail,
        string? clientApp,
        string? userAgent,
        string? locale)
    {
        Source = source;
        RegisteredAt = registeredAt;
        ViaReferral = viaReferral;
        InvitedByEmail = invitedByEmail;
        InvitedEmail = invitedEmail;
        ClientApp = clientApp;
        UserAgent = userAgent;
        Locale = locale;
    }

    public RegistrationSource Source { get; }

    public DateTimeOffset RegisteredAt { get; }

    /// <summary>Created from an invitation or reflink (D1, D7).</summary>
    public bool ViaReferral { get; }

    /// <summary>The inviter's email at the time of the invitation: a snapshot for audit, not kept in sync.</summary>
    public string? InvitedByEmail { get; }

    /// <summary>The address the invitation was sent to; may differ from the account's email (D8).</summary>
    public string? InvitedEmail { get; }

    public string? ClientApp { get; }

    public string? UserAgent { get; }

    /// <summary>The browser's language at sign-up, kept as sent (statistics); the profile has the normalised one.</summary>
    public string? Locale { get; }

    public static Registration Create(
        RegistrationSource source,
        DateTimeOffset registeredAt,
        bool viaReferral,
        string? invitedByEmail,
        string? invitedEmail,
        string? clientApp,
        string? userAgent,
        string? locale) =>
        new(
            source,
            registeredAt,
            viaReferral,
            Text.Sanitize(invitedByEmail, Email.MaxLength)?.ToLowerInvariant(),
            Text.Sanitize(invitedEmail, Email.MaxLength)?.ToLowerInvariant(),
            Text.Sanitize(clientApp, ClientAppMaxLength),
            Text.Sanitize(userAgent, UserAgentMaxLength),
            Text.Sanitize(locale, LocaleMaxLength));
}

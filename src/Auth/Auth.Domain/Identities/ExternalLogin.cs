using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>
/// A Google or Apple account linked to an identity. (Provider, Subject) is unique across Auth (R11);
/// an identity has at most one login per provider.
/// </summary>
public sealed record ExternalLogin
{
    public const int SubjectMaxLength = 255;

    private ExternalLogin(ExternalProvider provider, string subject, Email emailAtLink, DateTimeOffset linkedAt)
    {
        Provider = provider;
        Subject = subject;
        EmailAtLink = emailAtLink;
        LinkedAt = linkedAt;
    }

    public ExternalProvider Provider { get; }

    /// <summary>The provider's stable user id (<c>sub</c> in its ID token).</summary>
    public string Subject { get; }

    /// <summary>Email the provider reported when the login was linked; informational only.</summary>
    public Email EmailAtLink { get; }

    public DateTimeOffset LinkedAt { get; }

    public static Result<ExternalLogin> Create(ExternalProvider provider, string? subject, Email emailAtLink, DateTimeOffset linkedAt)
    {
        ArgumentNullException.ThrowIfNull(emailAtLink);

        if (string.IsNullOrWhiteSpace(subject) || subject.Length > SubjectMaxLength)
        {
            return Result.Fail<ExternalLogin>(IdentityErrors.ExternalSubjectInvalid);
        }

        return new ExternalLogin(provider, subject, emailAtLink, linkedAt);
    }
}

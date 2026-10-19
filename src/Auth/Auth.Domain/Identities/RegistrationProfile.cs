using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>
/// Profile data given at sign-up. Auth does not store it: it only travels in <c>IdentityRegistered</c> to Accounts,
/// which owns the profile and validates it fully.
/// </summary>
public sealed record RegistrationProfile
{
    public const int DisplayNameMaxLength = 100;

    private RegistrationProfile(string displayName, string? firstName, string? lastName, Uri? avatarUrl)
    {
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
    }

    public string DisplayName { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public Uri? AvatarUrl { get; }

    public static Result<RegistrationProfile> Create(string? displayName, string? firstName, string? lastName, Uri? avatarUrl)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > DisplayNameMaxLength)
        {
            return Result.Fail<RegistrationProfile>(IdentityErrors.DisplayNameRequired);
        }

        return new RegistrationProfile(name, Blank(firstName), Blank(lastName), avatarUrl);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Where the sign-up came from (R-kontekst): client app, user agent and locale. Never the IP address.</summary>
public sealed record RegistrationContext(string? ClientApp, string? UserAgent, string? Locale);

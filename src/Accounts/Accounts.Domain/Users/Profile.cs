using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>
/// What the user tells about themselves (accounts.md 3.1). Edited by the user or a higher role (R1). Always complete:
/// a PATCH is applied in the application layer and gives a whole new <see cref="Profile"/>.
/// </summary>
public sealed record Profile
{
    public static readonly DateOnly EarliestDateOfBirth = new(1900, 1, 1);

    private Profile(
        DisplayName displayName,
        PersonName? firstName,
        PersonName? lastName,
        PersonName? preferredName,
        AvatarUrl? avatarUrl,
        Locale locale,
        TimeZoneId timeZone,
        DateOnly? dateOfBirth)
    {
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        PreferredName = preferredName;
        AvatarUrl = avatarUrl;
        Locale = locale;
        TimeZone = timeZone;
        DateOfBirth = dateOfBirth;
    }

    public DisplayName DisplayName { get; }

    public PersonName? FirstName { get; }

    public PersonName? LastName { get; }

    /// <summary>How JARVIS should address the user.</summary>
    public PersonName? PreferredName { get; }

    public AvatarUrl? AvatarUrl { get; }

    public Locale Locale { get; }

    public TimeZoneId TimeZone { get; }

    public DateOnly? DateOfBirth { get; }

    /// <summary>Validates every field and returns all errors at once, so a form can mark each bad field.</summary>
    public static Result<Profile> Create(
        string? displayName,
        string? firstName,
        string? lastName,
        string? preferredName,
        string? avatarUrl,
        string? locale,
        string? timeZone,
        DateOnly? dateOfBirth,
        DateOnly today)
    {
        var name = DisplayName.Create(displayName);
        var first = PersonName.Create(firstName, "first_name");
        var last = PersonName.Create(lastName, "last_name");
        var preferred = PersonName.Create(preferredName, "preferred_name");
        var avatar = Users.AvatarUrl.Create(avatarUrl);
        var lang = Users.Locale.Create(locale);
        var zone = TimeZoneId.Create(timeZone);
        var birth = dateOfBirth is { } date && (date > today || date < EarliestDateOfBirth)
            ? Result.Fail(UserErrors.DateOfBirthInvalid)
            : Result.Ok();

        var all = Result.Merge(name, first, last, preferred, avatar, lang, zone, birth);
        if (all.IsFailed)
        {
            return all;
        }

        return new Profile(name.Value, first.Value, last.Value, preferred.Value, avatar.Value, lang.Value, zone.Value, dateOfBirth);
    }

    /// <summary>
    /// The starting profile from Auth's <c>IdentityRegistered</c>. Never fails: Auth already checked the input and an
    /// event that cannot be applied would block the queue. Unusable values are cleaned or dropped (accounts.md R7, Q3).
    /// </summary>
    public static Profile FromRegistration(string? displayName, string? firstName, string? lastName, string? avatarUrl, string? locale) =>
        new(
            DisplayName.FromRegistration(displayName),
            PersonName.FromRegistration(firstName),
            PersonName.FromRegistration(lastName),
            null,
            Users.AvatarUrl.FromRegistration(avatarUrl),
            Users.Locale.FromRegistration(locale),
            TimeZoneId.Default,
            null);

    /// <summary>Names of the fields that differ from <paramref name="other"/>, for the account history.</summary>
    public IReadOnlyList<string> ChangedFrom(Profile other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var changed = new List<string>();
        if (DisplayName != other.DisplayName) changed.Add(nameof(DisplayName));
        if (FirstName != other.FirstName) changed.Add(nameof(FirstName));
        if (LastName != other.LastName) changed.Add(nameof(LastName));
        if (PreferredName != other.PreferredName) changed.Add(nameof(PreferredName));
        if (AvatarUrl != other.AvatarUrl) changed.Add(nameof(AvatarUrl));
        if (Locale != other.Locale) changed.Add(nameof(Locale));
        if (TimeZone != other.TimeZone) changed.Add(nameof(TimeZone));
        if (DateOfBirth != other.DateOfBirth) changed.Add(nameof(DateOfBirth));
        return changed;
    }
}

using Households.Domain.Common;

namespace Households.Domain.Users;

/// <summary>
/// Households' copy of an account from Accounts (S1–S4): only what the household screens show and whether the account
/// is active (D6). Changed only by events from Accounts, so it never fails: names are cleaned like Accounts does.
/// </summary>
public sealed class UserCopy
{
    public const int DisplayNameMaxLength = 64;
    public const int AvatarUrlMaxLength = 2048;
    public const string FallbackName = "Użytkownik";

    private UserCopy() { }

    private UserCopy(Guid id) => Id = id;

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = FallbackName;

    public string? AvatarUrl { get; private set; }

    /// <summary>D6: false while the account is disabled; the member is shown greyed out and cannot take over a household (D5).</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>S1, from <c>UserCreated</c>.</summary>
    public static UserCopy Create(Guid id, string? displayName, string? avatarUrl)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The user id comes from Accounts and cannot be empty.", nameof(id));
        }

        var user = new UserCopy(id);
        user.UpdateProfile(displayName, avatarUrl);
        return user;
    }

    /// <summary>S2, from <c>UserProfileUpdated</c>.</summary>
    public void UpdateProfile(string? displayName, string? avatarUrl)
    {
        DisplayName = Text.Sanitize(displayName, DisplayNameMaxLength) ?? FallbackName;
        AvatarUrl = Text.Sanitize(avatarUrl, AvatarUrlMaxLength);
    }

    /// <summary>S3, from <c>UserDisabled</c>. A ban (disabled by an admin) also moves ownership: <see cref="Households.Household.OwnerBanned"/>.</summary>
    public void Disable() => IsActive = false;

    /// <summary>S3, from <c>UserEnabled</c>.</summary>
    public void Enable() => IsActive = true;
}

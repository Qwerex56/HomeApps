using System.Text.RegularExpressions;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>
/// An avatar picture address: an https URL (from Google or typed in) or the server's own path to an uploaded file,
/// <c>/users/v1/{userId}/avatar?v={timestamp}</c> (release 3.0, 2.15). The user id is in the path, never <c>/me</c>,
/// so another user or a plugin can fetch it.
/// </summary>
public sealed partial record AvatarUrl
{
    public const int MaxLength = 2048;

    private AvatarUrl(string value) => Value = value;

    public string Value { get; }

    /// <summary>The path of an uploaded avatar; <paramref name="version"/> changes with every upload so caches let go.</summary>
    public static AvatarUrl ForUpload(Guid userId, long version) => new($"/users/v1/{userId}/avatar?v={version}");

    /// <summary>Blank means "no avatar" and gives null.</summary>
    public static Result<AvatarUrl?> Create(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return Result.Ok<AvatarUrl?>(null);
        }

        if (value.Length > MaxLength)
        {
            return Result.Fail<AvatarUrl?>(UserErrors.AvatarUrlInvalid);
        }

        if (LocalPath().IsMatch(value))
        {
            return new AvatarUrl(value);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Fail<AvatarUrl?>(UserErrors.AvatarUrlInvalid);
        }

        return new AvatarUrl(uri.ToString());
    }

    /// <summary>From Google or Apple through Auth: a bad address is dropped rather than failing the registration.</summary>
    public static AvatarUrl? FromRegistration(string? raw)
    {
        var result = Create(raw);
        return result.IsSuccess ? result.Value : null;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^/users/v1/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/avatar(\?v=\d{1,20})?$")]
    private static partial Regex LocalPath();
}

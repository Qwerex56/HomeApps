using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>An avatar picture address; in 1.0 only a URL (from Google or typed in), no upload.</summary>
public sealed record AvatarUrl
{
    public const int MaxLength = 2048;

    private AvatarUrl(Uri value) => Value = value;

    public Uri Value { get; }

    /// <summary>Blank means "no avatar" and gives null.</summary>
    public static Result<AvatarUrl?> Create(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return Result.Ok<AvatarUrl?>(null);
        }

        if (value.Length > MaxLength
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Fail<AvatarUrl?>(UserErrors.AvatarUrlInvalid);
        }

        return new AvatarUrl(uri);
    }

    /// <summary>From Google or Apple through Auth: a bad address is dropped rather than failing the registration.</summary>
    public static AvatarUrl? FromRegistration(string? raw)
    {
        var result = Create(raw);
        return result.IsSuccess ? result.Value : null;
    }

    public override string ToString() => Value.ToString();
}

using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;

namespace Accounts.Application.Common;

/// <summary>
/// The whole profile form (Q5: PUT, not PATCH). Blank text clears an optional field. The domain checks the values;
/// the validator only checks the shape.
/// </summary>
public sealed record ProfileInput(
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? PreferredName,
    string? AvatarUrl,
    string? Locale,
    string? TimeZone,
    DateOnly? DateOfBirth)
{
    public Result<Profile> ToProfile(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return Profile.Create(DisplayName, FirstName, LastName, PreferredName, AvatarUrl, Locale, TimeZone, DateOfBirth, today);
    }
}

internal sealed class ProfileInputValidator : AbstractValidator<ProfileInput>
{
    public ProfileInputValidator()
    {
        RuleFor(p => p.DisplayName).NotEmpty();
        RuleFor(p => p.Locale).NotEmpty();
        RuleFor(p => p.TimeZone).NotEmpty();
    }
}

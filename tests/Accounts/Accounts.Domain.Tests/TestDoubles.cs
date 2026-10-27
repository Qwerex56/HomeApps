using Accounts.Domain.Common;
using Accounts.Domain.Users;

namespace Accounts.Domain.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    public static Email Email(string value = "adam@example.com") => Domain.Common.Email.Create(value).Value;

    public static Profile Profile(string displayName = "Adam", string? avatarUrl = null) =>
        Users.Profile.Create(displayName, "Adam", "Nowak", null, avatarUrl, "pl-PL", "Europe/Warsaw", null, Today).Value;

    public static string Code(this FluentResults.ResultBase result) =>
        (string)result.Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey];

    public static IEnumerable<string> Codes(this FluentResults.ResultBase result) =>
        result.Errors.Select(e => (string)e.Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey]);
}

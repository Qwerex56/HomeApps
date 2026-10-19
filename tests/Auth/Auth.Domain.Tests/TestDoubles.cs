using Auth.Domain.Common;
using Auth.Domain.Identities;

namespace Auth.Domain.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string hash, string password) => hash == Hash(password);
}

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    public const string Password = "correct horse battery";

    public static Email Email(string value = "adam@example.com") => Domain.Common.Email.Create(value).Value;

    public static RegistrationProfile Profile() => RegistrationProfile.Create("Adam", "Adam", "Nowak", null).Value;

    public static RegistrationContext Context() => new("homeapps-web", "Firefox", "pl-PL");

    public static ClientInfo Client() => ClientInfo.Create("homeapps-web", "Firefox").Value;

    /// <summary>A registered, active password identity with its first session; events cleared.</summary>
    public static Registration Registered(TimeProvider time, string email = "adam@example.com")
    {
        var registration = Identity.RegisterWithPassword(
            Email(email), Password, Profile(), Context(), Client(), false, true, new FakeHasher(), time).Value;
        registration.Identity.ClearDomainEvents();
        return registration;
    }

    public static string Code(this FluentResults.ResultBase result) =>
        (string)result.Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey];
}

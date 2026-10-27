using Accounts.Domain.Common;
using Accounts.Domain.Users;

namespace Accounts.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("  Adam  ", "Adam")]
    [InlineData("Jo", "Jo")]
    public void DisplayName_IsTrimmed(string raw, string expected) => Assert.Equal(expected, DisplayName.Create(raw).Value.Value);

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("A")]
    [InlineData("Ad\u0000am")]
    public void DisplayName_RejectsTooShortOrControlCharacters(string? raw) =>
        Assert.Equal("profile.display_name_invalid", DisplayName.Create(raw).Code());

    [Fact]
    public void DisplayName_RejectsMoreThan64Characters() =>
        Assert.True(DisplayName.Create(new string('a', DisplayName.MaxLength + 1)).IsFailed);

    [Fact]
    public void DisplayName_FromRegistrationNeverFails()
    {
        Assert.Equal(DisplayName.MaxLength, DisplayName.FromRegistration(new string('a', 100)).Value.Length);
        Assert.Equal(DisplayName.Fallback, DisplayName.FromRegistration("A").Value);
        Assert.Equal("Adam", DisplayName.FromRegistration(" Ad\nam ").Value);
    }

    [Fact]
    public void PersonName_BlankIsNoValue()
    {
        Assert.Null(PersonName.Create("  ", "first_name").Value);
        Assert.Equal("profile.first_name_invalid", PersonName.Create(new string('a', 65), "first_name").Code());
    }

    [Theory]
    [InlineData("pl-PL", "pl-PL")]
    [InlineData("PL_pl", "pl-PL")]
    [InlineData("en", "en")]
    [InlineData("sr-latn-rs", "sr-Latn-RS")]
    [InlineData("es-419", "es-419")]
    public void Locale_IsNormalised(string raw, string expected) => Assert.Equal(expected, Locale.Create(raw).Value.Value);

    [Theory]
    [InlineData("")]
    [InlineData("polski")]
    [InlineData("pl-PL-x")]
    public void Locale_RejectsWhatIsNotATag(string raw) => Assert.Equal("profile.locale_invalid", Locale.Create(raw).Code());

    [Theory]
    [InlineData("en-US,en;q=0.9", "en-US")]
    [InlineData("nonsense value", "pl-PL")]
    [InlineData(null, "pl-PL")]
    public void Q3_LocaleFromRegistrationFallsBackToPolish(string? raw, string expected) =>
        Assert.Equal(expected, Locale.FromRegistration(raw).Value);

    [Fact]
    public void TimeZone_AcceptsIanaOnly()
    {
        Assert.Equal("Europe/Warsaw", TimeZoneId.Create(" Europe/Warsaw ").Value.Value);
        Assert.Equal("profile.time_zone_invalid", TimeZoneId.Create("Mars/Olympus").Code());
        Assert.True(TimeZoneId.Create("").IsFailed);
    }

    [Theory]
    [InlineData("http://example.com/a.png")]
    [InlineData("example.com/a.png")]
    [InlineData("javascript:alert(1)")]
    public void AvatarUrl_MustBeHttps(string raw) => Assert.Equal("profile.avatar_url_invalid", AvatarUrl.Create(raw).Code());

    [Fact]
    public void AvatarUrl_BlankIsNoAvatarAndBadOneFromRegistrationIsDropped()
    {
        Assert.Null(AvatarUrl.Create(" ").Value);
        Assert.Null(AvatarUrl.FromRegistration("http://insecure.example.com/a.png"));
        Assert.NotNull(AvatarUrl.FromRegistration("https://lh3.googleusercontent.com/a"));
    }

    [Fact]
    public void Profile_ReturnsEveryErrorAtOnce()
    {
        var result = Profile.Create("A", null, null, null, "http://x", "??", "Nowhere", A.Today.AddDays(1), A.Today);

        Assert.Equal(
            ["profile.display_name_invalid", "profile.avatar_url_invalid", "profile.locale_invalid",
             "profile.time_zone_invalid", "profile.date_of_birth_invalid"],
            result.Codes());
    }

    [Fact]
    public void Profile_DateOfBirthBefore1900IsInvalid() =>
        Assert.True(Profile.Create("Adam", null, null, null, null, "pl", "Europe/Warsaw", new DateOnly(1899, 12, 31), A.Today).IsFailed);

    [Fact]
    public void Profile_FromRegistrationUsesDefaults()
    {
        var profile = Profile.FromRegistration("Adam", "Adam", " ", null, null);

        Assert.Equal("pl-PL", profile.Locale.Value);
        Assert.Equal("Europe/Warsaw", profile.TimeZone.Value);
        Assert.Null(profile.LastName);
    }

    [Fact]
    public void Profile_ListsChangedFields()
    {
        var before = A.Profile();
        var after = Profile.Create("Adam N.", "Adam", "Nowak", "Szefie", null, "pl-PL", "Europe/Warsaw", null, A.Today).Value;

        Assert.Equal(["DisplayName", "PreferredName"], after.ChangedFrom(before));
        Assert.Empty(A.Profile().ChangedFrom(before));
    }

    [Fact]
    public void Email_IsNormalised() => Assert.Equal("adam@example.com", Email.Create(" Adam@Example.COM ").Value.Value);
}

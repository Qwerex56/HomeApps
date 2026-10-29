using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;

namespace Households.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("D")]
    [InlineData("  Dom nad jeziorem  ")]
    [InlineData("12345678901234567890123456789012")]
    public void HouseholdNameAccepts1To32Characters(string raw) =>
        Assert.Equal(raw.Trim(), HouseholdName.Create(raw).Value.Value);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("123456789012345678901234567890123")]
    [InlineData("Dom\tLetni")]
    public void HouseholdNameRejectsEmptyTooLongOrControlCharacters(string? raw) =>
        Assert.Equal("household.name_invalid", HouseholdName.Create(raw).Code());

    [Fact]
    public void DescriptionIsOptionalAllowsNewLinesAndMax120()
    {
        Assert.Null(HouseholdDescription.Create("  ").Value);
        Assert.Equal("a\nb", HouseholdDescription.Create("a\r\nb").Value!.Value);
        Assert.Equal("household.description_invalid", HouseholdDescription.Create(new string('x', 121)).Code());
        Assert.Equal("household.description_invalid", HouseholdDescription.Create("a\tb").Code());
    }

    [Fact]
    public void NicknameIsOptionalUpTo32()
    {
        Assert.Null(Nickname.Create(null).Value);
        Assert.Equal("Mama", Nickname.Create(" Mama ").Value!.Value);
        Assert.Equal("household.nickname_invalid", Nickname.Create(new string('x', 33)).Code());
    }

    [Fact]
    public void GeneratedCodesAreTenCrockfordCharactersAndDiffer()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => InviteCode.Generate().Value).ToList();

        Assert.All(codes, c => Assert.Matches("^[0-9A-HJKMNP-TV-Z]{10}$", c));
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void CodeIsShownWithADashAndParsedLeniently()
    {
        var code = InviteCode.Parse("abcde-fghjk").Value;

        Assert.Equal("ABCDEFGHJK", code.Value);
        Assert.Equal("ABCDE-FGHJK", code.Display);
        Assert.Equal(InviteCode.Parse("ABCDEFGHJK").Value, code);
        Assert.Equal("01100-ABCDE", InviteCode.Parse("oIL0o abcde").Value.Display);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ABCDE")]
    [InlineData("ABCDE-FGHJKM")]
    [InlineData("ABCDE-FGHJU")]
    public void MalformedCodeIsNotFound(string? raw) =>
        Assert.Equal("invite.not_found", InviteCode.Parse(raw).Code());

    [Fact]
    public void ValidityDurationsMatchD3b()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), InviteValidity.ThirtyMinutes.Duration());
        Assert.Equal(TimeSpan.FromDays(7), InviteValidity.SevenDays.Duration());
    }

    [Fact]
    public void UserCopyCleansNamesInsteadOfFailing()
    {
        var user = UserCopy.Create(Guid.NewGuid(), "\u0007 ", null);
        Assert.Equal(UserCopy.FallbackName, user.DisplayName);

        user.UpdateProfile(" Ania\n" + new string('a', 100), "https://example.com/a.png");
        Assert.Equal(UserCopy.DisplayNameMaxLength, user.DisplayName.Length);
        Assert.StartsWith("Ania", user.DisplayName, StringComparison.Ordinal);
        Assert.Equal("https://example.com/a.png", user.AvatarUrl);

        user.Disable();
        Assert.False(user.IsActive);
        user.Enable();
        Assert.True(user.IsActive);
    }
}

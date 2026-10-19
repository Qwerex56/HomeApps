using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;

namespace Auth.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("  Adam@Example.COM ", "adam@example.com")]
    [InlineData("a.b+c@sub.example.org", "a.b+c@sub.example.org")]
    public void Email_IsTrimmedAndLowerCased(string raw, string expected) =>
        Assert.Equal(expected, Email.Create(raw).Value.Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("two@@example.com")]
    [InlineData("space in@example.com")]
    [InlineData("nodot@example")]
    public void Email_RejectsInvalid(string? raw) => Assert.True(Email.Create(raw).IsFailed);

    [Fact]
    public void Email_RejectsOver254Characters() =>
        Assert.True(Email.Create(new string('a', 250) + "@x.pl").IsFailed);

    [Fact]
    public void Email_EqualsByValue() => Assert.Equal(A.Email("ADAM@example.com"), A.Email("adam@example.com"));

    [Theory]
    [InlineData("short")]
    [InlineData("elevenchars")]
    [InlineData("adam@example.com")]
    public void R2_Password_RejectsTooShortOrEqualToEmail(string password) =>
        Assert.True(PasswordPolicy.Validate(password, A.Email()).IsFailed);

    [Fact]
    public void R2_Password_RejectsOver128Characters() =>
        Assert.True(PasswordPolicy.Validate(new string('x', 129), A.Email()).IsFailed);

    [Fact]
    public void R2_Password_NeedsNoCompositionRules() =>
        Assert.True(PasswordPolicy.Validate("only lower case words", A.Email()).IsSuccess);

    [Fact]
    public void OpaqueToken_StoresOnlyTheHashOfTheRawValue()
    {
        var (raw, hash) = OpaqueToken.New();

        Assert.NotEqual(raw, hash);
        Assert.Equal(hash, OpaqueToken.HashOf(raw));
        Assert.NotEqual(raw, OpaqueToken.New().Raw);
    }

    [Fact]
    public void ClientInfo_RequiresClientIdAndTrimsLongUserAgent()
    {
        Assert.True(ClientInfo.Create(" ", null).IsFailed);
        Assert.Equal(ClientInfo.UserAgentMaxLength, ClientInfo.Create("jarvis", new string('u', 600)).Value.UserAgent!.Length);
    }

    [Fact]
    public void RegistrationProfile_RequiresDisplayName()
    {
        Assert.True(RegistrationProfile.Create("  ", null, null, null).IsFailed);
        Assert.Null(RegistrationProfile.Create("Adam", " ", null, null).Value.FirstName);
    }

    [Fact]
    public void R5_Lockout_FifthFailureLocksFor15Minutes()
    {
        var lockout = Lockout.None;
        for (var i = 0; i < 4; i++)
        {
            lockout = lockout.RegisterFailure(A.Now);
        }

        Assert.False(lockout.IsLocked(A.Now));

        lockout = lockout.RegisterFailure(A.Now);

        Assert.True(lockout.IsLocked(A.Now.AddMinutes(14)));
        Assert.False(lockout.IsLocked(A.Now.AddMinutes(15)));
    }

    [Fact]
    public void R5_Lockout_CountsAgainFromOneAfterTheLockRunsOut()
    {
        var lockout = Lockout.None;
        for (var i = 0; i < 5; i++)
        {
            lockout = lockout.RegisterFailure(A.Now);
        }

        lockout = lockout.RegisterFailure(A.Now.AddMinutes(16));

        Assert.Equal(1, lockout.FailedAttempts);
        Assert.False(lockout.IsLocked(A.Now.AddMinutes(16)));
    }

    [Fact]
    public void InvitationCode_IsReadable()
    {
        var code = InvitationCode.New().Value;

        Assert.Matches("^[2-9A-HJ-NP-Z]{3}-[2-9A-HJ-NP-Z]{3}$", code);
    }
}

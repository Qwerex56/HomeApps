using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;

namespace Auth.Domain.Tests;

public sealed class CredentialTests
{
    private const string NewPassword = "a brand new password";

    private readonly FixedTime _time = new(A.Now);
    private readonly FakeHasher _hasher = new();

    private static T Last<T>(Identity identity) => identity.DomainEvents.OfType<T>().Last();

    private Identity GoogleOnly() => Identity.RegisterExternal(
        ExternalProvider.Google, "sub-1", A.Email("g@gmail.com"), A.Profile(), A.Context(), A.Client(),
        null, false, false, true, _time).Value.Identity;

    [Fact]
    public void A08_ResetLinkSetsPasswordUnlocksAndEndsSessions()
    {
        var identity = A.Registered(_time).Identity;
        for (var i = 0; i < 5; i++)
        {
            identity.SignInWithPassword("wrong password!!", A.Client(), _hasher, _time);
        }

        identity.RequestPasswordReset(_time);
        var raw = Last<PasswordResetRequested>(identity).RawToken;

        Assert.True(identity.ResetPassword(raw, NewPassword, _hasher, _time).IsSuccess);
        Assert.Equal(_hasher.Hash(NewPassword), identity.PasswordHash);
        Assert.True(identity.CanSignIn(A.Now));
        Assert.Empty(identity.ActiveSessions(A.Now));
        Assert.Null(identity.EmailToken);
    }

    [Fact]
    public void R9_LinkIsSingleUseAndExpiresAfterOneHour()
    {
        var identity = A.Registered(_time).Identity;
        identity.RequestPasswordReset(_time);
        var raw = Last<PasswordResetRequested>(identity).RawToken;

        _time.Now = A.Now.AddHours(1);
        Assert.Equal("token.invalid_or_expired", identity.ResetPassword(raw, NewPassword, _hasher, _time).Code());

        _time.Now = A.Now;
        identity.ResetPassword(raw, NewPassword, _hasher, _time);
        Assert.True(identity.ResetPassword(raw, NewPassword, _hasher, _time).IsFailed);
    }

    [Fact]
    public void A12_PasswordChangeIsOnlyARequestUntilConfirmed()
    {
        var identity = A.Registered(_time).Identity;
        var oldHash = identity.PasswordHash;

        Assert.True(identity.RequestPasswordChange(A.Password, NewPassword, A.Now, _hasher, _time).IsSuccess);
        Assert.Equal(oldHash, identity.PasswordHash);

        var raw = Last<PasswordChangeRequested>(identity).RawToken;
        Assert.True(identity.ConfirmPasswordChange(raw, _time).IsSuccess);
        Assert.Equal(_hasher.Hash(NewPassword), identity.PasswordHash);
        Assert.Empty(identity.ActiveSessions(A.Now));
    }

    [Fact]
    public void R8_PasswordChangeNeedsTheCurrentPassword() =>
        Assert.Equal("auth.invalid_credentials",
            A.Registered(_time).Identity.RequestPasswordChange("wrong password!!", NewPassword, A.Now, _hasher, _time).Code());

    [Fact]
    public void R8_GoogleOnlyAccountNeedsARecentSignInToSetPassword()
    {
        var identity = GoogleOnly();

        Assert.Equal("auth.reauthentication_required",
            identity.RequestPasswordChange(null, NewPassword, A.Now.AddMinutes(-6), _hasher, _time).Code());
        Assert.True(identity.RequestPasswordChange(null, NewPassword, A.Now.AddMinutes(-4), _hasher, _time).IsSuccess);

        identity.ConfirmPasswordChange(Last<PasswordChangeRequested>(identity).RawToken, _time);
        Assert.True(identity.HasPassword);
    }

    [Fact]
    public void P8_NewestRequestCancelsTheOlderOne()
    {
        var identity = A.Registered(_time).Identity;
        identity.RequestPasswordChange(A.Password, NewPassword, A.Now, _hasher, _time);
        var passwordLink = Last<PasswordChangeRequested>(identity).RawToken;

        identity.RequestEmailChange(A.Password, A.Now, A.Email("new@example.com"), false, _hasher, _time);

        Assert.True(identity.ConfirmPasswordChange(passwordLink, _time).IsFailed);
    }

    [Fact]
    public void A13_EmailChangesOnlyAfterTheLinkFromTheNewAddress()
    {
        var identity = A.Registered(_time).Identity;
        var newEmail = A.Email("new@example.com");

        identity.RequestEmailChange(A.Password, A.Now, newEmail, false, _hasher, _time);
        Assert.Equal(A.Email(), identity.Email);

        var requested = Last<EmailChangeRequested>(identity);
        Assert.Equal(newEmail, requested.NewEmail);

        Assert.True(identity.ConfirmEmailChange(requested.RawToken, false, _time).IsSuccess);
        Assert.Equal(newEmail, identity.Email);
        Assert.Equal(newEmail, Last<IdentityEmailChanged>(identity).Email);
    }

    [Fact]
    public void R1_EmailChangeChecksTheAddressAgainOnConfirm()
    {
        var identity = A.Registered(_time).Identity;
        Assert.Equal("email.taken",
            identity.RequestEmailChange(A.Password, A.Now, A.Email("new@example.com"), true, _hasher, _time).Code());
        Assert.Equal("email.unchanged",
            identity.RequestEmailChange(A.Password, A.Now, A.Email(), false, _hasher, _time).Code());

        identity.RequestEmailChange(A.Password, A.Now, A.Email("new@example.com"), false, _hasher, _time);

        Assert.Equal("email.taken",
            identity.ConfirmEmailChange(Last<EmailChangeRequested>(identity).RawToken, true, _time).Code());
    }

    [Fact]
    public void R10_PasswordResetForSomeoneNeedsAHigherRole()
    {
        var user = A.Registered(_time).Identity;

        Assert.Equal("forbidden.role", user.RequestPasswordResetBy(ServerRole.User, _time).Code());
        Assert.True(user.RequestPasswordResetBy(ServerRole.Admin, _time).IsSuccess);
        Assert.Empty(user.ActiveSessions(A.Now));
        Assert.Single(user.DomainEvents.OfType<PasswordResetRequested>());
    }

    [Fact]
    public void R11_AtMostOneLoginPerProvider()
    {
        var identity = A.Registered(_time).Identity;

        Assert.True(identity.LinkExternalLogin(ExternalProvider.Google, "sub-1", A.Email(), false, _time).IsSuccess);
        Assert.Equal("external_login.already_linked",
            identity.LinkExternalLogin(ExternalProvider.Google, "sub-2", A.Email(), false, _time).Code());
        Assert.Equal("external_login.already_linked",
            identity.LinkExternalLogin(ExternalProvider.Apple, "sub-3", A.Email(), true, _time).Code());
    }

    [Fact]
    public void R12_LastSignInMethodCannotBeRemoved()
    {
        var identity = GoogleOnly();

        Assert.Equal("external_login.last_method", identity.UnlinkExternalLogin(ExternalProvider.Google, _time).Code());

        var withPassword = A.Registered(_time).Identity;
        withPassword.LinkExternalLogin(ExternalProvider.Google, "sub-9", A.Email(), false, _time);
        Assert.True(withPassword.UnlinkExternalLogin(ExternalProvider.Google, _time).IsSuccess);
        Assert.Empty(withPassword.ExternalLogins);
    }
}

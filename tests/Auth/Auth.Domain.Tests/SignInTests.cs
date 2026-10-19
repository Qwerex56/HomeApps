using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;

namespace Auth.Domain.Tests;

public sealed class SignInTests
{
    private readonly FixedTime _time = new(A.Now);
    private readonly FakeHasher _hasher = new();

    private FluentResults.Result<SignInOutcome> SignIn(Identity identity, string password = A.Password) =>
        identity.SignInWithPassword(password, A.Client(), _hasher, _time);

    [Fact]
    public void A02_CorrectPasswordStartsNewSessionAndPublishesSignIn()
    {
        var identity = A.Registered(_time).Identity;
        _time.Now = A.Now.AddHours(1);

        var signedIn = Assert.IsType<SignInOutcome.SignedIn>(SignIn(identity).Value);

        Assert.Equal(2, identity.Sessions.Count);
        Assert.Equal(A.Now.AddHours(1), signedIn.Token.AuthenticatedAt);
        var evt = Assert.IsType<IdentitySignedIn>(Assert.Single(identity.DomainEvents));
        Assert.Equal(AuthMethod.Password, evt.Method);
    }

    [Fact]
    public void R4_WrongPasswordAndMissingPasswordGiveTheSameError()
    {
        var identity = A.Registered(_time).Identity;
        var googleOnly = Identity.RegisterExternal(
            ExternalProvider.Google, "sub", A.Email("g@gmail.com"), A.Profile(), A.Context(), A.Client(),
            null, false, false, true, _time).Value.Identity;

        Assert.Equal("auth.invalid_credentials", SignIn(identity, "wrong password!!").Code());
        Assert.Equal("auth.invalid_credentials", SignIn(googleOnly).Code());
    }

    [Fact]
    public void R5_FifthWrongPasswordLocksAndEvenTheRightOneIsRefused()
    {
        var identity = A.Registered(_time).Identity;
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal("auth.invalid_credentials", SignIn(identity, "wrong password!!").Code());
        }

        Assert.Equal("auth.locked_out", SignIn(identity, "wrong password!!").Code());
        Assert.Equal("auth.locked_out", SignIn(identity).Code());
        Assert.False(identity.CanSignIn(A.Now));

        _time.Now = A.Now.AddMinutes(15);
        Assert.True(SignIn(identity).IsSuccess);
    }

    [Fact]
    public void R5_SuccessResetsTheCounter()
    {
        var identity = A.Registered(_time).Identity;
        for (var i = 0; i < 4; i++)
        {
            SignIn(identity, "wrong password!!");
        }

        SignIn(identity);

        Assert.Equal(Lockout.None, identity.Lockout);
    }

    [Fact]
    public void R3_AdminDisabledAccountIsRefusedWithoutRevealingThePassword()
    {
        var identity = A.Registered(_time).Identity;
        identity.ApplyStatus(AccountStatus.DisabledByAdmin, _time);

        Assert.Equal("auth.cannot_sign_in", SignIn(identity).Code());
        Assert.Equal("auth.cannot_sign_in", SignIn(identity, "wrong password!!").Code());
    }

    [Fact]
    public void R16_SelfDisabledAccountGetsReactivationInsteadOfSession()
    {
        var identity = A.Registered(_time).Identity;
        identity.ApplyStatus(AccountStatus.DisabledBySelf, _time);

        var outcome = Assert.IsType<SignInOutcome.ReactivationRequired>(SignIn(identity).Value);

        Assert.Equal(AuthMethod.Password, outcome.Method);
        Assert.Empty(identity.ActiveSessions(A.Now));
        Assert.Equal("auth.invalid_credentials", SignIn(identity, "wrong password!!").Code());
    }

    [Fact]
    public void A09_ReactivationWaitsForAccountsThenStartsSession()
    {
        var identity = A.Registered(_time).Identity;
        identity.ApplyStatus(AccountStatus.DisabledBySelf, _time);

        Assert.Equal("reactivation.pending", identity.CompleteReactivation(AuthMethod.Password, A.Client(), _time).Code());

        identity.ApplyStatus(AccountStatus.Active, _time);
        var token = identity.CompleteReactivation(AuthMethod.Password, A.Client(), _time).Value;

        Assert.Contains(identity.ActiveSessions(A.Now), s => s.Id == token.SessionId);
        Assert.Contains(identity.DomainEvents, e => e is AccountReactivatedBySelf);
    }

    [Fact]
    public void A05_ExternalSignInNeedsTheLinkedLogin()
    {
        var identity = Identity.RegisterExternal(
            ExternalProvider.Google, "sub-1", A.Email("g@gmail.com"), A.Profile(), A.Context(), A.Client(),
            null, false, false, true, _time).Value.Identity;

        Assert.True(identity.SignInExternal(ExternalProvider.Google, "sub-1", A.Client(), _time).IsSuccess);
        Assert.True(identity.SignInExternal(ExternalProvider.Google, "other", A.Client(), _time).IsFailed);
        Assert.True(identity.SignInExternal(ExternalProvider.Apple, "sub-1", A.Client(), _time).IsFailed);
    }
}

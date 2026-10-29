using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;

namespace Auth.Domain.Tests;

public sealed class RefreshTests
{
    private readonly FixedTime _time = new(A.Now);

    [Fact]
    public void R6_RefreshRotatesInsideTheSameSession()
    {
        var (identity, t1) = A.Registered(_time);

        var t2 = identity.Refresh(t1.Raw, A.Client(), _time).Value;

        Assert.Equal(t1.SessionId, t2.SessionId);
        Assert.NotEqual(t1.Raw, t2.Raw);
        Assert.Equal(A.Now, t2.AuthenticatedAt);
        var session = Assert.Single(identity.Sessions);
        Assert.Equal(2, session.RefreshTokens.Count);
        Assert.NotNull(session.RefreshTokens[0].UsedAt);
    }

    [Fact]
    public void D5_EveryRotationExtendsBy30Days()
    {
        var (identity, t1) = A.Registered(_time);
        _time.Now = A.Now.AddDays(29);

        var t2 = identity.Refresh(t1.Raw, A.Client(), _time).Value;

        Assert.Equal(A.Now.AddDays(59), t2.ExpiresAt);
    }

    [Fact]
    public void D5_UnusedTokenExpiresAfter30Days()
    {
        var (identity, t1) = A.Registered(_time);
        _time.Now = A.Now.AddDays(30);

        Assert.Equal("auth.invalid_refresh_token", identity.Refresh(t1.Raw, A.Client(), _time).Code());
    }

    [Fact]
    public void R7_ReusedTokenRevokesTheWholeSession()
    {
        var (identity, t1) = A.Registered(_time);
        var t2 = identity.Refresh(t1.Raw, A.Client(), _time).Value;
        _time.Now = A.Now.AddSeconds(11);

        Assert.Equal("auth.invalid_refresh_token", identity.Refresh(t1.Raw, A.Client(), _time).Code());
        Assert.Equal("auth.invalid_refresh_token", identity.Refresh(t2.Raw, A.Client(), _time).Code());
        Assert.Contains(identity.DomainEvents, e => e is RefreshTokenReuseDetected);
        Assert.Empty(identity.ActiveSessions(A.Now));
    }

    [Fact]
    public void R6a_UsedTokenInsideTheGraceWindowRotatesAgainWithoutRevoking()
    {
        var (identity, t1) = A.Registered(_time);
        identity.Refresh(t1.Raw, A.Client(), _time);
        _time.Now = A.Now.AddSeconds(10);

        var again = identity.Refresh(t1.Raw, A.Client(), _time);

        Assert.True(again.IsSuccess);
        Assert.Equal(t1.SessionId, again.Value.SessionId);
        Assert.DoesNotContain(identity.DomainEvents, e => e is RefreshTokenReuseDetected);
        Assert.True(identity.Refresh(again.Value.Raw, A.Client(), _time).IsSuccess);
        Assert.Single(identity.ActiveSessions(_time.Now));
    }

    [Fact]
    public void R6a_GraceWindowComesFromTheCaller()
    {
        var (identity, t1) = A.Registered(_time);
        identity.Refresh(t1.Raw, A.Client(), _time);

        Assert.True(identity.Refresh(t1.Raw, A.Client(), _time, TimeSpan.Zero).IsFailed);
        Assert.Contains(identity.DomainEvents, e => e is RefreshTokenReuseDetected);
    }

    [Fact]
    public void R6a_AnotherClientInsideTheGraceWindowStillRevokes()
    {
        var (identity, t1) = A.Registered(_time);
        identity.Refresh(t1.Raw, A.Client(), _time);
        var jarvis = ClientInfo.Create("jarvis", null).Value;

        Assert.True(identity.Refresh(t1.Raw, jarvis, _time).IsFailed);
        Assert.Empty(identity.ActiveSessions(A.Now));
    }

    [Fact]
    public void R7_TokenFromAnotherClientRevokesTheSession()
    {
        var (identity, t1) = A.Registered(_time);

        var jarvis = ClientInfo.Create("jarvis", null).Value;

        Assert.True(identity.Refresh(t1.Raw, jarvis, _time).IsFailed);
        Assert.True(identity.Refresh(t1.Raw, A.Client(), _time).IsFailed);
    }

    [Fact]
    public void UnknownTokenFails()
    {
        var (identity, _) = A.Registered(_time);

        Assert.Equal("auth.invalid_refresh_token", identity.Refresh("not-a-token", A.Client(), _time).Code());
    }

    [Fact]
    public void R3_DisabledAccountCannotRefresh()
    {
        var (identity, t1) = A.Registered(_time);
        identity.ApplyStatus(AccountStatus.DisabledByAdmin, _time);

        Assert.True(identity.Refresh(t1.Raw, A.Client(), _time).IsFailed);
    }

    [Fact]
    public void A35_RefreshPublishesSignInAtMostEvery15Minutes()
    {
        var (identity, t1) = A.Registered(_time);
        var t2 = identity.Refresh(t1.Raw, A.Client(), _time).Value;
        Assert.Single(identity.DomainEvents.OfType<IdentitySignedIn>());

        _time.Now = A.Now.AddMinutes(10);
        var t3 = identity.Refresh(t2.Raw, A.Client(), _time).Value;
        Assert.Single(identity.DomainEvents.OfType<IdentitySignedIn>());

        _time.Now = A.Now.AddMinutes(16);
        identity.Refresh(t3.Raw, A.Client(), _time);
        Assert.Equal(2, identity.DomainEvents.OfType<IdentitySignedIn>().Count());
    }

    [Fact]
    public void A04_SignOutEndsOnlyThatSessionAndIsIdempotent()
    {
        var (identity, t1) = A.Registered(_time);
        identity.SignInWithPassword(A.Password, A.Client(), new FakeHasher(), _time);

        identity.SignOut(t1.SessionId, _time);
        identity.SignOut(t1.SessionId, _time);
        identity.SignOut(Guid.NewGuid(), _time);

        Assert.Single(identity.ActiveSessions(A.Now));
        Assert.True(identity.Refresh(t1.Raw, A.Client(), _time).IsFailed);
    }

    [Fact]
    public void A14_SignOutEverywhereEndsAllSessions()
    {
        var (identity, _) = A.Registered(_time);
        identity.SignInWithPassword(A.Password, A.Client(), new FakeHasher(), _time);

        identity.SignOutEverywhere(_time);

        Assert.Empty(identity.ActiveSessions(A.Now));
    }
}

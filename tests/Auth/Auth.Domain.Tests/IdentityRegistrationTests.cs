using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;
using Auth.Domain.Invitations;

namespace Auth.Domain.Tests;

public sealed class IdentityRegistrationTests
{
    private readonly FixedTime _time = new(A.Now);
    private readonly FakeHasher _hasher = new();

    private FluentResults.Result<Registration> RegisterWithPassword(
        string password = A.Password, bool emailTaken = false, bool registrationOpen = true) =>
        Identity.RegisterWithPassword(
            A.Email(), password, A.Profile(), A.Context(), A.Client(), emailTaken, registrationOpen, _hasher, _time);

    private FluentResults.Result<Registration> RegisterGoogle(
        Invitation? invitation = null, bool emailTaken = false, bool loginTaken = false, bool registrationOpen = true) =>
        Identity.RegisterExternal(
            ExternalProvider.Google, "google-sub-1", A.Email("adam@gmail.com"), A.Profile(), A.Context(), A.Client(),
            invitation, emailTaken, loginTaken, registrationOpen, _time);

    private Invitation Invite(string email = "invited@example.com") =>
        Invitation.Create(new Inviter(Guid.NewGuid(), A.Email("ewa@example.com"), true), A.Email(email), false, false, _time)
            .Value.Invitation;

    [Fact]
    public void A01_RegisterWithPassword_CreatesActiveUserWithHashedPasswordAndFirstSession()
    {
        var registration = RegisterWithPassword().Value;
        var identity = registration.Identity;

        Assert.Equal(_hasher.Hash(A.Password), identity.PasswordHash);
        Assert.Equal(ServerRole.User, identity.Role);
        Assert.Equal(RegistrationSource.Password, identity.Source);
        Assert.True(identity.CanSignIn(A.Now));

        var session = Assert.Single(identity.Sessions);
        Assert.Equal(registration.RefreshToken.SessionId, session.Id);
        Assert.Equal(AuthMethod.Password, session.Method);
        Assert.Equal(A.Now, session.StartedAt);
        Assert.True(session.IsActive(A.Now));
        Assert.Equal(OpaqueToken.HashOf(registration.RefreshToken.Raw), Assert.Single(session.RefreshTokens).TokenHash);
        Assert.Equal(A.Now.AddDays(30), registration.RefreshToken.ExpiresAt);
    }

    [Fact]
    public void A01_RegisterWithPassword_RaisesIdentityRegisteredWithProfile()
    {
        var identity = RegisterWithPassword().Value.Identity;

        var registered = Assert.IsType<IdentityRegistered>(Assert.Single(identity.DomainEvents));
        Assert.Equal(identity.Id, registered.IdentityId);
        Assert.Equal("Adam", registered.Profile.DisplayName);
        Assert.Null(registered.InvitedByEmail);
    }

    [Fact]
    public void R1_TakenEmailIsRejected() =>
        Assert.Equal("email.taken", RegisterWithPassword(emailTaken: true).Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey]);

    [Fact]
    public void R2_WeakPasswordIsRejected() => Assert.True(RegisterWithPassword("short").IsFailed);

    [Fact]
    public void R15_ClosedRegistrationRejectsSignUpWithoutInvitation()
    {
        Assert.True(RegisterWithPassword(registrationOpen: false).IsFailed);
        Assert.True(RegisterGoogle(registrationOpen: false).IsFailed);
    }

    [Fact]
    public void A07_InvitationWithPassword_UsesInvitedEmailAndAcceptsInvitation()
    {
        var invitation = Invite();

        var identity = Identity.AcceptInvitationWithPassword(
            invitation, A.Password, A.Profile(), A.Context(), A.Client(), false, _hasher, _time).Value.Identity;

        Assert.Equal(A.Email("invited@example.com"), identity.Email);
        Assert.Equal(InvitationStatus.Accepted, invitation.StatusAt(A.Now));
        Assert.Equal(identity.Id, invitation.AcceptedByIdentityId);
        var registered = Assert.IsType<IdentityRegistered>(Assert.Single(identity.DomainEvents));
        Assert.Equal(A.Email("ewa@example.com"), registered.InvitedByEmail);
        Assert.Equal(A.Email("invited@example.com"), registered.InvitedEmail);
    }

    [Fact]
    public void A07_UsedInvitationCannotRegisterTwice()
    {
        var invitation = Invite();
        Identity.AcceptInvitationWithPassword(invitation, A.Password, A.Profile(), A.Context(), A.Client(), false, _hasher, _time);

        Assert.True(Identity.AcceptInvitationWithPassword(
            invitation, A.Password, A.Profile(), A.Context(), A.Client(), false, _hasher, _time).IsFailed);
    }

    [Fact]
    public void A30_BootstrapInvitationMakesOwner()
    {
        var invitation = Invitation.CreateBootstrap(A.Email("owner@example.com"), _time).Invitation;

        var identity = Identity.AcceptInvitationWithPassword(
            invitation, A.Password, A.Profile(), A.Context(), A.Client(), false, _hasher, _time).Value.Identity;

        Assert.Equal(ServerRole.Owner, identity.Role);
        Assert.Equal(RegistrationSource.Bootstrap, identity.Source);
    }

    [Fact]
    public void A05_RegisterExternal_LinksLoginWithoutPassword()
    {
        var identity = RegisterGoogle().Value.Identity;

        Assert.False(identity.HasPassword);
        Assert.Equal(RegistrationSource.Google, identity.Source);
        var login = Assert.Single(identity.ExternalLogins);
        Assert.Equal(("google-sub-1", ExternalProvider.Google), (login.Subject, login.Provider));
        Assert.Equal(AuthMethod.Google, Assert.Single(identity.Sessions).Method);
    }

    [Fact]
    public void R14_D8_InvitationAcceptedWithGoogleUnderDifferentEmail()
    {
        var invitation = Invite("invited@example.com");

        var identity = RegisterGoogle(invitation, registrationOpen: false).Value.Identity;

        Assert.Equal(A.Email("adam@gmail.com"), identity.Email);
        var registered = Assert.IsType<IdentityRegistered>(Assert.Single(identity.DomainEvents));
        Assert.Equal(A.Email("invited@example.com"), registered.InvitedEmail);
        Assert.Equal(InvitationStatus.Accepted, invitation.StatusAt(A.Now));
    }

    [Fact]
    public void R11_ExternalLoginAlreadyLinkedIsRejected() => Assert.True(RegisterGoogle(loginTaken: true).IsFailed);

    [Fact]
    public void R17_ExternalSignUpWithTakenEmailIsRejected() => Assert.True(RegisterGoogle(emailTaken: true).IsFailed);
}

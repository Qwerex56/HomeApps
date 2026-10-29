using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using Auth.Domain.Invitations.Events;

namespace Auth.Domain.Tests;

public sealed class InvitationTests
{
    private readonly FixedTime _time = new(A.Now);

    private static Inviter ActiveInviter() => new(Guid.NewGuid(), A.Email("ewa@example.com"), CanSignIn: true);

    [Fact]
    public void Create_StoresOnlyTheTokenHashAndRaisesInvitationCreated()
    {
        var issued = Invitation.Create(ActiveInviter(), A.Email(), false, false, _time).Value;
        var invitation = issued.Invitation;

        Assert.True(invitation.Matches(issued.RawToken));
        Assert.NotEqual(issued.RawToken, invitation.TokenHash);
        Assert.Equal(A.Now.AddDays(7), invitation.ExpiresAt);
        Assert.Equal(A.Email("ewa@example.com"), invitation.InvitedByEmail);
        var created = Assert.IsType<InvitationCreated>(Assert.Single(invitation.DomainEvents));
        Assert.Equal(issued.RawToken, created.RawToken);
    }

    [Fact]
    public void R10_InactiveInviterCannotInvite() =>
        Assert.True(Invitation.Create(ActiveInviter() with { CanSignIn = false }, A.Email(), false, false, _time).IsFailed);

    [Fact]
    public void R14_EmailWithIdentityOrPendingInvitationCannotBeInvited()
    {
        Assert.Contains(IdentityErrors.EmailTaken.Message,
            Invitation.Create(ActiveInviter(), A.Email(), true, false, _time).Errors.Select(e => e.Message));
        Assert.Contains(InvitationErrors.AlreadyPending.Message,
            Invitation.Create(ActiveInviter(), A.Email(), false, true, _time).Errors.Select(e => e.Message));
    }

    [Fact]
    public void Status_IsDerivedFromDates()
    {
        var invitation = Invitation.CreateBootstrap(A.Email(), _time).Invitation;

        Assert.Equal(InvitationStatus.Pending, invitation.StatusAt(A.Now.AddDays(6)));
        Assert.Equal(InvitationStatus.Expired, invitation.StatusAt(A.Now.AddDays(7)));
    }

    [Fact]
    public void R14_AcceptIsSingleUse()
    {
        var invitation = Invitation.CreateBootstrap(A.Email(), _time).Invitation;
        var identityId = Guid.NewGuid();

        Assert.True(invitation.Accept(identityId, A.Now).IsSuccess);
        Assert.Equal(InvitationStatus.Accepted, invitation.StatusAt(A.Now));
        Assert.Equal(identityId, invitation.AcceptedByIdentityId);
        Assert.True(invitation.Accept(Guid.NewGuid(), A.Now).IsFailed);
    }

    [Fact]
    public void R14_ExpiredInvitationCannotBeAccepted()
    {
        var invitation = Invitation.CreateBootstrap(A.Email(), _time).Invitation;

        Assert.True(invitation.Accept(Guid.NewGuid(), A.Now.AddDays(8)).IsFailed);
    }

    [Fact]
    public void A21_ResendIssuesNewLinkAndExtendsAlsoWhenExpired()
    {
        var inviter = ActiveInviter();
        var issued = Invitation.Create(inviter, A.Email(), false, false, _time).Value;
        var invitation = issued.Invitation;
        _time.Now = A.Now.AddDays(10);

        var raw = invitation.Resend(inviter.UserId, Common.ServerRole.User, _time).Value;

        Assert.False(invitation.Matches(issued.RawToken));
        Assert.True(invitation.Matches(raw));
        Assert.Equal(InvitationStatus.Pending, invitation.StatusAt(_time.Now));
        Assert.Equal(2, invitation.DomainEvents.Count);
    }

    [Fact]
    public void R10_OnlyInviterOrAdminManagesAnInvitation()
    {
        var invitation = Invitation.Create(ActiveInviter(), A.Email(), false, false, _time).Value.Invitation;

        Assert.True(invitation.Revoke(Guid.NewGuid(), Common.ServerRole.User, _time).IsFailed);
        Assert.True(invitation.Resend(Guid.NewGuid(), Common.ServerRole.User, _time).IsFailed);
        Assert.True(invitation.Revoke(Guid.NewGuid(), Common.ServerRole.Admin, _time).IsSuccess);
        Assert.Equal(InvitationStatus.Revoked, invitation.StatusAt(A.Now));
    }

    [Fact]
    public void A21_AcceptedOrRevokedInvitationCannotBeResent()
    {
        var inviter = ActiveInviter();
        var invitation = Invitation.Create(inviter, A.Email(), false, false, _time).Value.Invitation;
        invitation.Revoke(inviter.UserId, Common.ServerRole.User, _time);

        Assert.True(invitation.Resend(inviter.UserId, Common.ServerRole.User, _time).IsFailed);
        Assert.True(invitation.Revoke(inviter.UserId, Common.ServerRole.User, _time).IsFailed);
    }

    [Fact]
    public void A30_BootstrapInvitationIsNotManagedThroughTheApi() =>
        Assert.True(Invitation.CreateBootstrap(A.Email(), _time).Invitation
            .Resend(Guid.NewGuid(), Common.ServerRole.Owner, _time).IsFailed);

    [Fact]
    public void P7_ForgetInviteeClearsOnlyTheInvitedAddress()
    {
        var invitation = Invitation.Create(ActiveInviter(), A.Email(), false, false, _time).Value.Invitation;

        invitation.ForgetInvitee();

        Assert.Null(invitation.Email);
        Assert.NotNull(invitation.InvitedByEmail);
    }
}

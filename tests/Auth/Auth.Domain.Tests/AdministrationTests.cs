using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;

namespace Auth.Domain.Tests;

public sealed class AdministrationTests
{
    private readonly FixedTime _time = new(A.Now);
    private readonly Guid _ownerId = Guid.NewGuid();

    [Fact]
    public void A23_OwnerGrantsAndRemovesAdmin()
    {
        var identity = A.Registered(_time).Identity;

        Assert.True(identity.ChangeRole(_ownerId, ServerRole.Owner, ServerRole.Admin, _time).IsSuccess);
        Assert.Equal(ServerRole.Admin, identity.Role);
        Assert.Equal(ServerRole.Admin, Assert.IsType<IdentityRoleChanged>(Assert.Single(identity.DomainEvents)).Role);

        identity.ChangeRole(_ownerId, ServerRole.Owner, ServerRole.User, _time);
        Assert.Equal(ServerRole.User, identity.Role);
    }

    [Fact]
    public void R18_OnlyOwnerChangesRolesAndNeverToOrFromOwner()
    {
        var identity = A.Registered(_time).Identity;

        Assert.Equal("forbidden.role", identity.ChangeRole(_ownerId, ServerRole.Admin, ServerRole.Admin, _time).Code());
        Assert.Equal("forbidden.role", identity.ChangeRole(_ownerId, ServerRole.Owner, ServerRole.Owner, _time).Code());
        Assert.Equal("forbidden.role", identity.ChangeRole(identity.Id, ServerRole.Owner, ServerRole.Admin, _time).Code());
    }

    [Fact]
    public void R18_OwnerRoleCannotBeTakenAway()
    {
        var invitation = Invitations.Invitation.CreateBootstrap(A.Email("owner@example.com"), _time).Invitation;
        var owner = Identity.AcceptInvitationWithPassword(
            invitation, A.Password, A.Profile(), A.Context(), A.Client(), false, new FakeHasher(), _time).Value.Identity;

        Assert.True(owner.ChangeRole(_ownerId, ServerRole.Owner, ServerRole.User, _time).IsFailed);
    }

    [Fact]
    public void SameRoleIsANoOp()
    {
        var identity = A.Registered(_time).Identity;

        Assert.True(identity.ChangeRole(_ownerId, ServerRole.Owner, ServerRole.User, _time).IsSuccess);
        Assert.Empty(identity.DomainEvents);
    }

    [Fact]
    public void A32_DisablingEndsSessionsAndIsIdempotent()
    {
        var identity = A.Registered(_time).Identity;

        identity.ApplyStatus(AccountStatus.DisabledByAdmin, _time);
        identity.ApplyStatus(AccountStatus.DisabledByAdmin, _time);

        Assert.False(identity.CanSignIn(A.Now));
        Assert.Empty(identity.ActiveSessions(A.Now));

        identity.ApplyStatus(AccountStatus.Active, _time);
        Assert.True(identity.CanSignIn(A.Now));
    }
}

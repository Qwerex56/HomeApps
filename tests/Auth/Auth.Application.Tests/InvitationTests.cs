using Auth.Application.Invitations;
using Auth.Domain.Common;
using Auth.Domain.Invitations;

namespace Auth.Application.Tests;

public sealed class InvitationTests
{
    private readonly World _w = new();

    private CreateInvitationHandler Create() => new(_w.Identities, _w.Invitations, _w.CurrentUser, _w.Time);

    private async Task<(Invitation Invitation, string RawToken)> Invited(string email = "ola@example.com")
    {
        var (adam, _) = await _w.Registered();
        _w.SignInAs(adam);
        await Create().Handle(new CreateInvitationCommand(email), default);
        var invitation = _w.Invitations.Items.Single();
        var created = Assert.IsType<Domain.Invitations.Events.InvitationCreated>(invitation.DomainEvents.Single());
        return (invitation, created.RawToken);
    }

    [Fact]
    public async Task Any_active_account_invites_and_gets_the_code_back()
    {
        var (adam, _) = await _w.Registered();
        _w.SignInAs(adam);

        var result = await Create().Handle(new CreateInvitationCommand("Ola@Example.com"), default);

        var invitation = Assert.Single(_w.Invitations.Items);
        Assert.Equal(invitation.Code.Value, result.Value.Code);
        Assert.Equal("adam@example.com", invitation.InvitedByEmail!.Value);
    }

    [Fact]
    public async Task Email_with_an_account_or_a_pending_invitation_is_refused()
    {
        await Invited();

        Assert.Equal("invitation.already_pending", (await Create().Handle(new CreateInvitationCommand("ola@example.com"), default)).Code());
        Assert.Equal("email.taken", (await Create().Handle(new CreateInvitationCommand("adam@example.com"), default)).Code());
    }

    [Fact]
    public async Task Preview_shows_who_invites_until_the_invitation_is_used()
    {
        var (invitation, raw) = await Invited();
        var preview = new GetInvitationPreviewHandler(_w.Invitations, _w.Time);

        var shown = await preview.Handle(new GetInvitationPreviewQuery(raw), default);
        Assert.Equal("adam@example.com", shown.Value.InvitedByEmail);
        Assert.Equal("ola@example.com", shown.Value.Email);

        _w.Time.Now = invitation.ExpiresAt;
        Assert.Equal("invitation.invalid_or_expired", (await preview.Handle(new GetInvitationPreviewQuery(raw), default)).Code());
    }

    [Fact]
    public async Task Accepting_with_a_password_registers_the_invited_email_through_closed_registration()
    {
        var (invitation, raw) = await Invited();
        _w.Options.RegistrationOpen = false;
        var accept = new AcceptInvitationWithPasswordHandler(_w.Identities, _w.Invitations, _w.Clients, _w.Tokens, _w.Hasher, _w.Time);

        var result = await accept.Handle(
            new AcceptInvitationWithPasswordCommand(raw, World.Password, "Ola", null, null, "pl-PL", "homeapps-web", null), default);

        Assert.True(result.IsSuccess);
        Assert.Contains(_w.Identities.Items, i => i.Email.Value == "ola@example.com");
        Assert.Equal(InvitationStatus.Accepted, invitation.StatusAt(_w.Time.Now));

        var again = await accept.Handle(
            new AcceptInvitationWithPasswordCommand(raw, World.Password, "Ola", null, null, null, "homeapps-web", null), default);
        Assert.Equal("invitation.invalid_or_expired", again.Code());
    }

    [Fact]
    public async Task Users_list_their_own_invitations_and_only_admins_list_all()
    {
        await Invited();
        var (ola, _) = await _w.Registered("other@example.com");
        _w.SignInAs(ola);
        var list = new ListInvitationsHandler(_w.Invitations, _w.CurrentUser, _w.Time);

        Assert.Empty((await list.Handle(new ListInvitationsQuery(false, null), default)).Value);
        Assert.Equal("forbidden.role", (await list.Handle(new ListInvitationsQuery(true, null), default)).Code());

        _w.CurrentUser.Role = ServerRole.Admin;
        Assert.Single((await list.Handle(new ListInvitationsQuery(true, InvitationStatus.Pending), default)).Value);
        Assert.Empty((await list.Handle(new ListInvitationsQuery(true, InvitationStatus.Accepted), default)).Value);
    }

    [Fact]
    public async Task Inviter_resends_and_revokes_but_a_stranger_cannot()
    {
        var (invitation, raw) = await Invited();
        var resend = new ResendInvitationHandler(_w.Invitations, _w.CurrentUser, _w.Time);
        var revoke = new RevokeInvitationHandler(_w.Invitations, _w.CurrentUser, _w.Time);

        Assert.True((await resend.Handle(new ResendInvitationCommand(invitation.Id), default)).IsSuccess);
        Assert.False(invitation.Matches(raw));

        _w.CurrentUser.UserId = Guid.NewGuid();
        Assert.Equal("forbidden.role", (await revoke.Handle(new RevokeInvitationCommand(invitation.Id), default)).Code());
        Assert.Equal("invitation.not_found", (await revoke.Handle(new RevokeInvitationCommand(Guid.NewGuid()), default)).Code());
    }

    [Fact]
    public async Task Bootstrap_invitation_is_created_once_and_never_once_an_owner_exists()
    {
        _w.Options.Bootstrap.Email = "owner@example.com";
        var ensure = new EnsureBootstrapInvitationHandler(_w.Identities, _w.Invitations, _w.OptionsAccessor, _w.Time);

        await ensure.Handle(new EnsureBootstrapInvitationCommand(), default);
        await ensure.Handle(new EnsureBootstrapInvitationCommand(), default);
        Assert.Equal(InvitationKind.Bootstrap, Assert.Single(_w.Invitations.Items).Kind);

        _w.Time.Now += Invitation.Lifetime;
        await ensure.Handle(new EnsureBootstrapInvitationCommand(), default);
        Assert.Equal(2, _w.Invitations.Items.Count);
    }

    [Fact]
    public async Task Bootstrap_does_nothing_without_configuration()
    {
        var ensure = new EnsureBootstrapInvitationHandler(_w.Identities, _w.Invitations, _w.OptionsAccessor, _w.Time);

        Assert.True((await ensure.Handle(new EnsureBootstrapInvitationCommand(), default)).IsSuccess);
        Assert.Empty(_w.Invitations.Items);
    }
}

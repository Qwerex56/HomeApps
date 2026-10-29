using Auth.Application.AccountsSync;
using Auth.Application.Admin;
using Auth.Application.MyAccount;
using Auth.Application.Password;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Identities.Events;
using Auth.Domain.Invitations;

namespace Auth.Application.Tests;

public sealed class AccountTests
{
    private readonly World _w = new();

    [Fact]
    public async Task Forgot_password_succeeds_for_any_email_and_mails_only_real_accounts()
    {
        var (adam, _) = await _w.Registered();
        var handler = new ForgotPasswordHandler(_w.Identities, _w.Time);

        Assert.True((await handler.Handle(new ForgotPasswordCommand("nobody@example.com"), default)).IsSuccess);
        Assert.True((await handler.Handle(new ForgotPasswordCommand("not an email"), default)).IsSuccess);
        Assert.Empty(adam.DomainEvents);

        await handler.Handle(new ForgotPasswordCommand("adam@example.com"), default);
        var requested = Assert.IsType<PasswordResetRequested>(Assert.Single(adam.DomainEvents));

        var reset = await new ResetPasswordHandler(_w.Identities, _w.Hasher, _w.Time)
            .Handle(new ResetPasswordCommand(requested.RawToken, "a brand new password"), default);
        Assert.True(reset.IsSuccess);
        Assert.Equal(_w.Hasher.Hash("a brand new password"), adam.PasswordHash);
    }

    [Fact]
    public async Task Password_change_waits_for_the_link_from_the_email()
    {
        var (adam, _) = await _w.Registered();
        _w.SignInAs(adam);

        var request = await new RequestPasswordChangeHandler(_w.Identities, _w.CurrentUser, _w.Hasher, _w.Time)
            .Handle(new RequestPasswordChangeCommand(World.Password, "a brand new password"), default);
        Assert.True(request.IsSuccess);
        Assert.Equal(_w.Hasher.Hash(World.Password), adam.PasswordHash);

        var raw = Assert.IsType<PasswordChangeRequested>(Assert.Single(adam.DomainEvents)).RawToken;
        var confirm = new ConfirmPasswordChangeHandler(_w.Identities, _w.Time);
        Assert.True((await confirm.Handle(new ConfirmPasswordChangeCommand(raw), default)).IsSuccess);
        Assert.Equal(_w.Hasher.Hash("a brand new password"), adam.PasswordHash);
        Assert.Equal("token.invalid_or_expired", (await confirm.Handle(new ConfirmPasswordChangeCommand(raw), default)).Code());
    }

    [Fact]
    public async Task Google_only_account_must_have_signed_in_recently_to_set_a_password()
    {
        await new External.SignInExternalHandler(
                _w.Identities, _w.Invitations, _w.Verifier, _w.Clients, _w.Tokens, _w.OptionsAccessor, _w.Time)
            .Handle(new External.SignInExternalCommand(
                ExternalProvider.Google, "c", "v", new Uri("https://home.test/cb"), null, null, "homeapps-web", null), default);
        _w.SignInAs(_w.Identities.Items[0]);
        var handler = new RequestPasswordChangeHandler(_w.Identities, _w.CurrentUser, _w.Hasher, _w.Time);

        _w.Time.Now += Identity.ReauthenticationWindow + TimeSpan.FromSeconds(1);
        var stale = await handler.Handle(new RequestPasswordChangeCommand(null, "a brand new password"), default);
        Assert.Equal("auth.reauthentication_required", stale.Code());

        _w.CurrentUser.AuthenticatedAt = _w.Time.Now;
        Assert.True((await handler.Handle(new RequestPasswordChangeCommand(null, "a brand new password"), default)).IsSuccess);
    }

    [Fact]
    public async Task Email_change_is_confirmed_from_the_new_address_and_rechecks_it_is_free()
    {
        var (adam, _) = await _w.Registered();
        _w.SignInAs(adam);

        await new RequestEmailChangeHandler(_w.Identities, _w.CurrentUser, _w.Hasher, _w.Time)
            .Handle(new RequestEmailChangeCommand("adam.new@example.com", World.Password), default);
        var raw = Assert.IsType<EmailChangeRequested>(Assert.Single(adam.DomainEvents)).RawToken;

        await _w.Registered("adam.new@example.com");
        var confirm = new ConfirmEmailChangeHandler(_w.Identities, _w.Time);
        Assert.Equal("email.taken", (await confirm.Handle(new ConfirmEmailChangeCommand(raw), default)).Code());
        Assert.Equal("adam@example.com", adam.Email.Value);
    }

    [Fact]
    public async Task Me_lists_my_methods_and_sessions_and_revokes_one()
    {
        var (adam, _) = await _w.Registered();
        _w.SignInAs(adam);

        var me = await new GetMeHandler(_w.Identities, _w.CurrentUser).Handle(new GetMeQuery(), default);
        Assert.True(me.Value.HasPassword);
        Assert.Empty(me.Value.Logins);

        var sessions = await new ListSessionsHandler(_w.Identities, _w.CurrentUser, _w.Time).Handle(new ListSessionsQuery(), default);
        var session = Assert.Single(sessions.Value);
        Assert.Equal("Firefox", session.UserAgent);

        await new RevokeSessionHandler(_w.Identities, _w.CurrentUser, _w.Time).Handle(new RevokeSessionCommand(session.Id), default);
        Assert.Empty(adam.ActiveSessions(_w.Time.Now));
    }

    [Fact]
    public async Task Last_way_of_signing_in_cannot_be_unlinked()
    {
        var handler = new External.SignInExternalHandler(
            _w.Identities, _w.Invitations, _w.Verifier, _w.Clients, _w.Tokens, _w.OptionsAccessor, _w.Time);
        await handler.Handle(new External.SignInExternalCommand(
            ExternalProvider.Google, "c", "v", new Uri("https://home.test/cb"), null, null, "homeapps-web", null), default);
        _w.SignInAs(_w.Identities.Items[0]);

        var result = await new UnlinkExternalLoginHandler(_w.Identities, _w.CurrentUser, _w.Time)
            .Handle(new UnlinkExternalLoginCommand(ExternalProvider.Google), default);

        Assert.Equal("external_login.last_method", result.Code());
    }

    [Fact]
    public async Task Admin_resets_a_users_password_and_the_owner_changes_roles()
    {
        var (adam, _) = await _w.Registered();
        var (ola, _) = await _w.Registered("ola@example.com");
        _w.CurrentUser.UserId = Guid.NewGuid();

        _w.CurrentUser.Role = ServerRole.Admin;
        Assert.True((await new ResetPasswordForHandler(_w.Identities, _w.CurrentUser, _w.Time)
            .Handle(new ResetPasswordForCommand(adam.Id), default)).IsSuccess);
        Assert.Empty(adam.ActiveSessions(_w.Time.Now));

        var changeRole = new ChangeRoleHandler(_w.Identities, _w.CurrentUser, _w.Time);
        Assert.Equal("forbidden.role", (await changeRole.Handle(new ChangeRoleCommand(ola.Id, ServerRole.Admin), default)).Code());

        _w.CurrentUser.Role = ServerRole.Owner;
        Assert.True((await changeRole.Handle(new ChangeRoleCommand(ola.Id, ServerRole.Admin), default)).IsSuccess);
        Assert.Equal(ServerRole.Admin, ola.Role);
        Assert.Equal("identity.not_found", (await changeRole.Handle(new ChangeRoleCommand(Guid.NewGuid(), ServerRole.Admin), default)).Code());
    }

    [Fact]
    public async Task Status_from_accounts_is_applied_and_unknown_users_are_ignored()
    {
        var (adam, _) = await _w.Registered();
        var handler = new ApplyAccountStatusHandler(_w.Identities, _w.Time);

        Assert.True((await handler.Handle(new ApplyAccountStatusCommand(Guid.NewGuid(), AccountStatus.DisabledByAdmin), default)).IsSuccess);
        await handler.Handle(new ApplyAccountStatusCommand(adam.Id, AccountStatus.DisabledByAdmin), default);

        Assert.Equal(AccountStatus.DisabledByAdmin, adam.Status);
        Assert.Empty(adam.ActiveSessions(_w.Time.Now));
    }

    [Theory]
    [InlineData(HomeApps.Contracts.Households.OwnershipTransferReason.OwnerBanned, true)]
    [InlineData(HomeApps.Contracts.Households.OwnershipTransferReason.OwnerErased, true)]
    [InlineData(HomeApps.Contracts.Households.OwnershipTransferReason.HandedOver, false)]
    public async Task New_household_owner_is_mailed_only_when_they_did_not_choose_it(
        HomeApps.Contracts.Households.OwnershipTransferReason reason, bool mailed)
    {
        var (adam, _) = await _w.Registered();
        var handler = new HouseholdsSync.NotifyOwnershipReceivedHandler(_w.Identities, _w.Bus);
        var household = Guid.NewGuid();

        Assert.True((await handler.Handle(new HouseholdsSync.NotifyOwnershipReceivedCommand(household, "Dom", adam.Id, reason), default)).IsSuccess);
        Assert.True((await handler.Handle(new HouseholdsSync.NotifyOwnershipReceivedCommand(household, "Dom", Guid.NewGuid(), reason), default)).IsSuccess);

        var emails = _w.Bus.Published.OfType<Events.AuthEmailRequested>()
            .Where(e => e.Template == Events.AuthEmailTemplate.HouseholdOwnershipReceived).ToList();
        Assert.Equal(mailed ? 1 : 0, emails.Count);
        if (mailed)
        {
            Assert.Equal((adam.Email.Value, household, "Dom"), (emails[0].To, emails[0].HouseholdId, emails[0].HouseholdName!));
        }
    }

    [Fact]
    public async Task Erasing_removes_the_identity_and_only_the_invitation_it_came_through_forgets_its_email()
    {
        var (adam, _) = await _w.Registered();
        var sent = Invitation.Create(new Inviter(adam.Id, adam.Email, true), Email.Create("x@example.com").Value, false, false, _w.Time).Value;
        _w.Invitations.Add(sent.Invitation);
        var bootstrap = Invitation.CreateBootstrap(Email.Create("ola@example.com").Value, _w.Time);
        _w.Invitations.Add(bootstrap.Invitation);
        await new Invitations.AcceptInvitationWithPasswordHandler(_w.Identities, _w.Invitations, _w.Clients, _w.Tokens, _w.Hasher, _w.Time)
            .Handle(new Invitations.AcceptInvitationWithPasswordCommand(
                bootstrap.RawToken, World.Password, "Ola", null, null, null, "homeapps-web", null), default);
        var ola = _w.Identities.Items.Single(i => i.Email.Value == "ola@example.com");
        var handler = new EraseIdentityHandler(_w.Identities, _w.Invitations);

        await handler.Handle(new EraseIdentityCommand(ola.Id), default);
        await handler.Handle(new EraseIdentityCommand(adam.Id), default);
        Assert.True((await handler.Handle(new EraseIdentityCommand(adam.Id), default)).IsSuccess);

        Assert.Empty(_w.Identities.Items);
        Assert.Null(bootstrap.Invitation.Email);
        Assert.Equal("x@example.com", sent.Invitation.Email!.Value);
        Assert.Equal("adam@example.com", sent.Invitation.InvitedByEmail!.Value);
    }
}

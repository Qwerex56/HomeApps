using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.External;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;

namespace Auth.Application.Tests;

public sealed class ExternalTests
{
    private readonly World _w = new();

    private SignInExternalHandler Handler() =>
        new(_w.Identities, _w.Invitations, _w.Verifier, _w.Clients, _w.Tokens, _w.OptionsAccessor, _w.Time);

    private static SignInExternalCommand Google(string? invitation = null) =>
        new(ExternalProvider.Google, "code", "verifier", new Uri("https://home.test/cb"), invitation, "pl-PL", "homeapps-web", null);

    [Fact]
    public async Task New_person_is_registered_with_the_providers_profile()
    {
        var result = await Handler().Handle(Google(), default);

        Assert.IsType<SignInResult.SignedIn>(result.Value);
        var identity = Assert.Single(_w.Identities.Items);
        Assert.Equal(RegistrationSource.Google, identity.Source);
        var registered = Assert.IsType<Domain.Identities.Events.IdentityRegistered>(identity.DomainEvents[0]);
        Assert.Equal("Ewa Kowalska", registered.Profile.DisplayName);
    }

    [Fact]
    public async Task Known_login_signs_in_without_creating_anything()
    {
        await Handler().Handle(Google(), default);

        var result = await Handler().Handle(Google(), default);

        Assert.IsType<SignInResult.SignedIn>(result.Value);
        Assert.Single(_w.Identities.Items);
        Assert.Equal(2, _w.Identities.Items[0].Sessions.Count);
    }

    [Fact]
    public async Task Email_of_an_existing_account_asks_to_link_and_changes_nothing()
    {
        var (identity, _) = await _w.Registered("ewa@example.com");

        var result = await Handler().Handle(Google(), default);

        var link = Assert.IsType<SignInResult.LinkRequired>(result.Value);
        Assert.Equal("ewa@example.com", link.Email);
        Assert.Empty(identity.ExternalLogins);
        Assert.Single(_w.Identities.Items);
    }

    [Fact]
    public async Task Unverified_email_is_refused()
    {
        _w.Verifier.Next = new ExternalIdentity("sub", "ewa@example.com", false, null, null, null);

        var result = await Handler().Handle(Google(), default);

        Assert.Equal("external_login.email_not_verified", result.Code());
    }

    [Fact]
    public async Task Unknown_invitation_token_is_refused_even_with_open_registration()
    {
        var result = await Handler().Handle(Google("forged"), default);

        Assert.Equal("invitation.invalid_or_expired", result.Code());
    }

    [Fact]
    public async Task Invitation_lets_a_google_sign_up_through_closed_registration_with_another_email()
    {
        _w.Options.RegistrationOpen = false;
        var issued = Invitation.CreateBootstrap(Domain.Common.Email.Create("owner@example.com").Value, _w.Time);
        _w.Invitations.Add(issued.Invitation);

        var result = await Handler().Handle(Google(issued.RawToken), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Domain.Common.ServerRole.Owner, _w.Identities.Items[0].Role);
        Assert.Equal(InvitationStatus.Accepted, issued.Invitation.StatusAt(_w.Time.Now));
    }

    [Fact]
    public async Task Confirming_the_link_with_the_right_password_links_and_signs_in()
    {
        var (identity, _) = await _w.Registered("ewa@example.com");
        var ticket = Assert.IsType<SignInResult.LinkRequired>((await Handler().Handle(Google(), default)).Value).Ticket;
        var confirm = new ConfirmExternalLinkHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Hasher, _w.UnitOfWork, _w.Time);

        var wrong = await confirm.Handle(new ConfirmExternalLinkCommand(ticket, "wrong password!", "homeapps-web", null), default);
        Assert.Equal("auth.invalid_credentials", wrong.Code());
        Assert.Equal(1, _w.UnitOfWork.Saves);
        Assert.Empty(identity.ExternalLogins);

        var right = await confirm.Handle(new ConfirmExternalLinkCommand(ticket, World.Password, "homeapps-web", null), default);
        Assert.IsType<SignInResult.SignedIn>(right.Value);
        Assert.Single(identity.ExternalLogins);
    }

    [Fact]
    public async Task Signed_in_user_links_google_and_the_same_login_cannot_go_to_someone_else()
    {
        var (adam, _) = await _w.Registered();
        var (ola, _) = await _w.Registered("ola@example.com");
        var handler = new LinkExternalLoginHandler(_w.Identities, _w.Verifier, _w.Time);
        LinkExternalLoginCommand For(Identity i) =>
            new(i.Id, ExternalProvider.Google, "code", "verifier", new Uri("https://home.test/cb"));

        Assert.True((await handler.Handle(For(adam), default)).IsSuccess);
        Assert.Equal("external_login.already_linked", (await handler.Handle(For(ola), default)).Code());
    }
}

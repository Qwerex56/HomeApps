using Auth.Application.Common;
using Auth.Application.Login;
using Auth.Application.Logout;
using Auth.Application.Reactivation;
using Auth.Application.Refresh;
using Auth.Application.Register;
using Auth.Domain.Identities;

namespace Auth.Application.Tests;

public sealed class SignInTests
{
    private readonly World _w = new();

    private SignInWithPasswordHandler Login() => new(_w.Identities, _w.Clients, _w.Tokens, _w.Hasher, _w.UnitOfWork, _w.Time);

    private RefreshTokensHandler Refresh() => new(_w.Identities, _w.Clients, _w.Tokens, _w.UnitOfWork, _w.Time);

    [Fact]
    public async Task Register_signs_the_new_user_in_with_the_clients_token_delivery()
    {
        var (identity, tokens) = await _w.Registered();

        Assert.Equal(ServerRoleOf(tokens), identity.Role.ToString());
        Assert.Equal(Abstractions.ClientType.Browser, tokens.ClientType);
        Assert.Single(identity.Sessions);
    }

    [Fact]
    public async Task Register_with_an_unknown_client_fails_before_anything_is_created()
    {
        var handler = new RegisterWithPasswordHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Hasher, _w.OptionsAccessor, _w.Time);

        var result = await handler.Handle(
            new RegisterWithPasswordCommand("a@example.com", World.Password, "A", null, null, null, "evil", null), CancellationToken.None);

        Assert.Equal("client.unknown", result.Code());
        Assert.Empty(_w.Identities.Items);
    }

    [Fact]
    public async Task Register_is_refused_when_registration_is_closed()
    {
        _w.Options.RegistrationOpen = false;
        var handler = new RegisterWithPasswordHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Hasher, _w.OptionsAccessor, _w.Time);

        var result = await handler.Handle(
            new RegisterWithPasswordCommand("a@example.com", World.Password, "A", null, null, null, "homeapps-web", null), CancellationToken.None);

        Assert.Equal("auth.registration_closed", result.Code());
    }

    [Fact]
    public async Task Unknown_email_still_verifies_a_hash_and_gives_the_generic_error()
    {
        var result = await Login().Handle(new SignInWithPasswordCommand("nobody@example.com", "whatever", "homeapps-web", null), default);

        Assert.Equal("auth.invalid_credentials", result.Code());
        Assert.Equal(1, _w.Hasher.Verifications);
    }

    [Fact]
    public async Task Wrong_password_fails_but_is_saved_for_the_lockout()
    {
        var (identity, _) = await _w.Registered();

        var result = await Login().Handle(new SignInWithPasswordCommand(identity.Email.Value, "wrong password!", "homeapps-web", null), default);

        Assert.Equal("auth.invalid_credentials", result.Code());
        Assert.Equal(1, _w.UnitOfWork.Saves);
        Assert.NotEqual(Lockout.None, identity.Lockout);
    }

    [Fact]
    public async Task Correct_password_starts_a_session()
    {
        var (identity, _) = await _w.Registered();

        var result = await Login().Handle(new SignInWithPasswordCommand("ADAM@example.com ", World.Password, "jarvis", "cli"), default);

        var signedIn = Assert.IsType<SignInResult.SignedIn>(result.Value);
        Assert.Equal(Abstractions.ClientType.Native, signedIn.Tokens.ClientType);
        Assert.Equal(2, identity.Sessions.Count);
        Assert.Equal(0, _w.UnitOfWork.Saves);
    }

    [Fact]
    public async Task Self_disabled_account_gets_a_reactivation_ticket_then_tokens_once_enabled()
    {
        var (identity, _) = await _w.Registered();
        identity.ApplyStatus(AccountStatus.DisabledBySelf, _w.Time);

        var login = await Login().Handle(new SignInWithPasswordCommand(identity.Email.Value, World.Password, "homeapps-web", null), default);
        var ticket = Assert.IsType<SignInResult.ReactivationRequired>(login.Value).Ticket;

        var complete = new CompleteReactivationHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Time);
        var command = new CompleteReactivationCommand(ticket, "homeapps-web", null);

        Assert.Equal("reactivation.pending", (await complete.Handle(command, default)).Code());

        identity.ApplyStatus(AccountStatus.Active, _w.Time);
        Assert.True((await complete.Handle(command, default)).IsSuccess);
    }

    [Fact]
    public async Task Reactivation_rejects_a_ticket_of_another_kind()
    {
        var ticket = _w.Tokens.IssueTicket(new Abstractions.ExternalLinkTicket(Guid.NewGuid(), ExternalProvider.Google, "s", "a@b.cd"));

        var result = await new CompleteReactivationHandler(_w.Identities, _w.Clients, _w.Tokens, _w.Time)
            .Handle(new CompleteReactivationCommand(ticket, "homeapps-web", null), default);

        Assert.Equal("ticket.invalid_or_expired", result.Code());
    }

    [Fact]
    public async Task Refresh_rotates_and_a_reused_token_revokes_the_session_and_is_saved()
    {
        var (identity, tokens) = await _w.Registered();

        var first = await Refresh().Handle(new RefreshTokensCommand(tokens.RefreshToken, "homeapps-web", null), default);
        Assert.True(first.IsSuccess);
        Assert.NotEqual(tokens.RefreshToken, first.Value.RefreshToken);

        var reused = await Refresh().Handle(new RefreshTokensCommand(tokens.RefreshToken, "homeapps-web", null), default);

        Assert.Equal("auth.invalid_refresh_token", reused.Code());
        Assert.Equal(1, _w.UnitOfWork.Saves);
        Assert.Empty(identity.ActiveSessions(_w.Time.Now));
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_fails()
    {
        var result = await Refresh().Handle(new RefreshTokensCommand("nope", "homeapps-web", null), default);

        Assert.Equal("auth.invalid_refresh_token", result.Code());
    }

    [Fact]
    public async Task Sign_out_ends_the_tokens_session_and_ignores_unknown_tokens()
    {
        var (identity, tokens) = await _w.Registered();
        var handler = new SignOutHandler(_w.Identities, _w.Time);

        Assert.True((await handler.Handle(new SignOutCommand("unknown"), default)).IsSuccess);
        Assert.True((await handler.Handle(new SignOutCommand(tokens.RefreshToken), default)).IsSuccess);

        Assert.Empty(identity.ActiveSessions(_w.Time.Now));
    }

    private static string ServerRoleOf(AuthTokens tokens) => tokens.AccessToken.Split(':')[2];
}

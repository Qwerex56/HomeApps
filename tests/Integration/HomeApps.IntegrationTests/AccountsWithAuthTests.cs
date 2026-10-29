using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeApps.IntegrationTests;

/// <summary>Accounts plan step 5.2: what only works when Auth and Accounts really talk through RabbitMQ.</summary>
public sealed class AccountsWithAuthTests(HomeAppsHost apps) : IClassFixture<HomeAppsHost>
{
    [Fact]
    public async Task Sign_up_in_auth_creates_the_account_in_accounts()
    {
        var person = await apps.SignUpAsync("Ewa");

        var me = await apps.AccountAsync(person.Tokens.Access);

        Assert.Equal(person.Email, me.GetProperty("email").GetString());
        Assert.Equal("Ewa", me.GetProperty("profile").GetProperty("displayName").GetString());
        Assert.Equal("User", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Self_disabled_account_comes_back_with_the_ticket()
    {
        var person = await apps.SignUpAsync("Kuba");
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await apps.AccountAsync(person.Tokens.Access);

        using var disable = await accounts.SendAsync(HttpMethod.Post, "/users/me/disable", person.Tokens.Access, new { reason = "Przerwa" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        var ticket = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var login = await auth.PostAsJsonAsync("/auth/login", HomeAppsHost.Login(person.Email));
            return login.StatusCode == HttpStatusCode.Forbidden ? (await login.ReadJsonAsync()).GetProperty("ticket").GetString() : null;
        }, "UserDisabled to reach Auth");

        var form = await (await accounts.SendAsync(HttpMethod.Get, "/users/me/reactivation", ticket)).ReadJsonAsync();
        Assert.Equal("Kuba", form.GetProperty("displayName").GetString());
        using var reactivate = await accounts.SendAsync(HttpMethod.Post, "/users/me/reactivate", ticket, JsonSerializer.Deserialize<object>(form.GetRawText()));
        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);

        var tokens = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var complete = await auth.PostAsJsonAsync("/auth/reactivation/complete", new { ticket, clientId = HomeAppsHost.Client });
            return complete.StatusCode == HttpStatusCode.OK ? await HomeAppsHost.TokensAsync(complete) : null;
        }, "UserEnabled to reach Auth");
        Assert.Equal("Active", (await apps.AccountAsync(tokens.Access)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Admin_disabling_ends_the_session_in_auth_and_enabling_lets_the_user_back()
    {
        var person = await apps.SignUpAsync("Zenek");
        var owner = await apps.OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await apps.AccountAsync(person.Tokens.Access);

        using var disable = await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{person.Id}/disable", owner.Access, new { reason = "Spam" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        // Disabled by an admin: no ticket, just "this account cannot sign in".
        await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var login = await auth.PostAsJsonAsync("/auth/login", HomeAppsHost.Login(person.Email));
            return (await login.ReadJsonAsync()).TryGetProperty("code", out var code) && code.GetString() == "auth.cannot_sign_in"
                ? (object)true
                : null;
        }, "UserDisabled to reach Auth");

        // Used for the first time only now, so a 401 means the session was ended, not a reused token.
        using var refresh = await auth.PostAsJsonAsync("/auth/refresh", new { clientId = HomeAppsHost.Client, refreshToken = person.Tokens.Refresh });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        using var enable = await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{person.Id}/enable", owner.Access);
        Assert.Equal(HttpStatusCode.NoContent, enable.StatusCode);
        await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var login = await auth.PostAsJsonAsync("/auth/login", HomeAppsHost.Login(person.Email));
            return login.StatusCode == HttpStatusCode.OK ? (object)true : null;
        }, "sign-in to work again");
    }

    [Fact]
    public async Task Role_changed_in_auth_is_copied_to_accounts()
    {
        var person = await apps.SignUpAsync("Ola");
        var owner = await apps.OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await apps.AccountAsync(person.Tokens.Access);

        using var change = await auth.SendAsync(HttpMethod.Put, $"/auth/admin/identities/{person.Id}/role", owner.Access, new { role = "Admin" });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        await HomeAppsHost.EventuallyAsync(async () =>
        {
            var user = await (await accounts.SendAsync(HttpMethod.Get, $"/admin/users/{person.Id}", owner.Access)).ReadJsonAsync();
            return user.GetProperty("role").GetString() == "Admin" ? (object)true : null;
        }, "IdentityRoleChanged to reach Accounts");
    }

    [Fact]
    public async Task Erasing_in_accounts_removes_the_identity_in_auth()
    {
        var person = await apps.SignUpAsync("Zapomniany");
        var owner = await apps.OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await apps.AccountAsync(person.Tokens.Access);
        await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{person.Id}/disable", owner.Access, new { });

        using var erase = await accounts.SendAsync(HttpMethod.Delete, $"/admin/users/{person.Id}", owner.Access, new { confirmEmail = person.Email });
        Assert.Equal(HttpStatusCode.NoContent, erase.StatusCode);

        // The email is free again only once the identity is gone (login alone was refused already while disabled).
        var again = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var signUp = await auth.PostAsJsonAsync("/auth/register", HomeAppsHost.SignUp(person.Email, "Nowy"));
            return signUp.StatusCode == HttpStatusCode.OK ? await HomeAppsHost.TokensAsync(signUp) : null;
        }, "UserErased to reach Auth");
        var me = await (await auth.SendAsync(HttpMethod.Get, "/auth/me", again.Access)).ReadJsonAsync();
        Assert.NotEqual(person.Id, me.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Confirmed_email_change_in_auth_reaches_accounts()
    {
        var person = await apps.SignUpAsync("Marek");
        using var auth = apps.Auth.CreateClient();
        await apps.AccountAsync(person.Tokens.Access);
        var newEmail = HomeAppsHost.Unique("marek.nowy");

        using var request = await auth.SendAsync(HttpMethod.Put, "/auth/me/email", person.Tokens.Access,
            new { newEmail, currentPassword = HomeAppsHost.Password });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var token = await apps.Emails.TokenSentToAsync(newEmail);
        using var confirm = await auth.PostAsJsonAsync("/auth/me/email/confirm", new { token });
        Assert.True(confirm.IsSuccessStatusCode, await confirm.Content.ReadAsStringAsync());

        using var login = await auth.PostAsJsonAsync("/auth/login", HomeAppsHost.Login(newEmail));
        var tokens = await HomeAppsHost.TokensAsync(login);
        await HomeAppsHost.EventuallyAsync(async () =>
            (await apps.AccountAsync(tokens.Access)).GetProperty("email").GetString() == newEmail ? (object)true : null,
            "IdentityEmailChanged to reach Accounts");
    }
}

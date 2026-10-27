using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeApps.IntegrationTests;

/// <summary>Accounts plan step 5.2: what only works when Auth and Accounts really talk through RabbitMQ.</summary>
public sealed class AccountsWithAuthTests(HomeAppsHost apps) : IClassFixture<HomeAppsHost>
{
    private sealed record Tokens(string Access, string Refresh);

    private sealed record Person(Guid Id, string Email, Tokens Tokens);

    [Fact]
    public async Task Sign_up_in_auth_creates_the_account_in_accounts()
    {
        var person = await SignUpAsync("Ewa");

        var me = await AccountAsync(person.Tokens.Access);

        Assert.Equal(person.Email, me.GetProperty("email").GetString());
        Assert.Equal("Ewa", me.GetProperty("profile").GetProperty("displayName").GetString());
        Assert.Equal("User", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Self_disabled_account_comes_back_with_the_ticket()
    {
        var person = await SignUpAsync("Kuba");
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await AccountAsync(person.Tokens.Access);

        using var disable = await accounts.SendAsync(HttpMethod.Post, "/users/me/disable", person.Tokens.Access, new { reason = "Przerwa" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);

        var ticket = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var login = await auth.PostAsJsonAsync("/auth/login", Login(person.Email));
            return login.StatusCode == HttpStatusCode.Forbidden ? (await login.ReadJsonAsync()).GetProperty("ticket").GetString() : null;
        }, "UserDisabled to reach Auth");

        var form = await (await accounts.SendAsync(HttpMethod.Get, "/users/me/reactivation", ticket)).ReadJsonAsync();
        Assert.Equal("Kuba", form.GetProperty("displayName").GetString());
        using var reactivate = await accounts.SendAsync(HttpMethod.Post, "/users/me/reactivate", ticket, JsonSerializer.Deserialize<object>(form.GetRawText()));
        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);

        var tokens = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var complete = await auth.PostAsJsonAsync("/auth/reactivation/complete", new { ticket, clientId = HomeAppsHost.Client });
            return complete.StatusCode == HttpStatusCode.OK ? await TokensAsync(complete) : null;
        }, "UserEnabled to reach Auth");
        Assert.Equal("Active", (await AccountAsync(tokens.Access)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Admin_disabling_ends_the_session_in_auth_and_enabling_lets_the_user_back()
    {
        var person = await SignUpAsync("Zenek");
        var owner = await OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await AccountAsync(person.Tokens.Access);

        using var disable = await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{person.Id}/disable", owner.Access, new { reason = "Spam" });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        // Disabled by an admin: no ticket, just "this account cannot sign in".
        await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var login = await auth.PostAsJsonAsync("/auth/login", Login(person.Email));
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
            using var login = await auth.PostAsJsonAsync("/auth/login", Login(person.Email));
            return login.StatusCode == HttpStatusCode.OK ? (object)true : null;
        }, "sign-in to work again");
    }

    [Fact]
    public async Task Role_changed_in_auth_is_copied_to_accounts()
    {
        var person = await SignUpAsync("Ola");
        var owner = await OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await AccountAsync(person.Tokens.Access);

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
        var person = await SignUpAsync("Zapomniany");
        var owner = await OwnerAsync();
        using var accounts = apps.Accounts.CreateClient();
        using var auth = apps.Auth.CreateClient();
        await AccountAsync(person.Tokens.Access);
        await accounts.SendAsync(HttpMethod.Post, $"/admin/users/{person.Id}/disable", owner.Access, new { });

        using var erase = await accounts.SendAsync(HttpMethod.Delete, $"/admin/users/{person.Id}", owner.Access, new { confirmEmail = person.Email });
        Assert.Equal(HttpStatusCode.NoContent, erase.StatusCode);

        // The email is free again only once the identity is gone (login alone was refused already while disabled).
        var again = await HomeAppsHost.EventuallyAsync(async () =>
        {
            using var signUp = await auth.PostAsJsonAsync("/auth/register", SignUp(person.Email, "Nowy"));
            return signUp.StatusCode == HttpStatusCode.OK ? await TokensAsync(signUp) : null;
        }, "UserErased to reach Auth");
        var me = await (await auth.SendAsync(HttpMethod.Get, "/auth/me", again.Access)).ReadJsonAsync();
        Assert.NotEqual(person.Id, me.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Confirmed_email_change_in_auth_reaches_accounts()
    {
        var person = await SignUpAsync("Marek");
        using var auth = apps.Auth.CreateClient();
        await AccountAsync(person.Tokens.Access);
        var newEmail = HomeAppsHost.Unique("marek.nowy");

        using var request = await auth.SendAsync(HttpMethod.Put, "/auth/me/email", person.Tokens.Access,
            new { newEmail, currentPassword = HomeAppsHost.Password });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var token = await apps.Emails.TokenSentToAsync(newEmail);
        using var confirm = await auth.PostAsJsonAsync("/auth/me/email/confirm", new { token });
        Assert.True(confirm.IsSuccessStatusCode, await confirm.Content.ReadAsStringAsync());

        using var login = await auth.PostAsJsonAsync("/auth/login", Login(newEmail));
        var tokens = await TokensAsync(login);
        await HomeAppsHost.EventuallyAsync(async () =>
            (await AccountAsync(tokens.Access)).GetProperty("email").GetString() == newEmail ? (object)true : null,
            "IdentityEmailChanged to reach Accounts");
    }

    private static object SignUp(string email, string displayName) =>
        new { email, password = HomeAppsHost.Password, displayName, locale = "pl-PL", clientId = HomeAppsHost.Client };

    private static object Login(string email) => new { email, password = HomeAppsHost.Password, clientId = HomeAppsHost.Client };

    private async Task<Person> SignUpAsync(string displayName)
    {
        var email = HomeAppsHost.Unique(displayName.ToLowerInvariant());
        using var auth = apps.Auth.CreateClient();
        using var registered = await auth.PostAsJsonAsync("/auth/register", SignUp(email, displayName));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var tokens = await TokensAsync(registered);
        var me = await (await auth.SendAsync(HttpMethod.Get, "/auth/me", tokens.Access)).ReadJsonAsync();
        return new Person(me.GetProperty("id").GetGuid(), email, tokens);
    }

    /// <summary>The account in Accounts, waiting while it says <c>user.pending</c> (UC-01).</summary>
    private async Task<JsonElement> AccountAsync(string accessToken)
    {
        using var accounts = apps.Accounts.CreateClient();
        return await HomeAppsHost.EventuallyAsync<JsonElement?>(async () =>
        {
            using var response = await accounts.SendAsync(HttpMethod.Get, "/users/me", accessToken);
            return response.StatusCode == HttpStatusCode.OK ? await response.ReadJsonAsync() : null;
        }, "the account in Accounts") ?? default;
    }

    private static SemaphoreSlim OwnerLock { get; } = new(1, 1);

    private static Tokens? _owner;

    /// <summary>The Owner invited at start-up (A-30): accepts the emailed invitation once, then signs in.</summary>
    private async Task<Tokens> OwnerAsync()
    {
        await OwnerLock.WaitAsync();
        try
        {
            using var auth = apps.Auth.CreateClient();
            if (_owner is null)
            {
                var token = await apps.Emails.TokenSentToAsync(HomeAppsHost.OwnerEmail);
                using var accepted = await auth.PostAsJsonAsync($"/auth/invitations/{token}/accept", new
                {
                    password = HomeAppsHost.Password,
                    displayName = "Gospodarz",
                    clientId = HomeAppsHost.Client,
                });
                Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
                _owner = await TokensAsync(accepted);
            }

            using var login = await auth.PostAsJsonAsync("/auth/login", Login(HomeAppsHost.OwnerEmail));
            return await TokensAsync(login);
        }
        finally
        {
            OwnerLock.Release();
        }
    }

    private static async Task<Tokens> TokensAsync(HttpResponseMessage response)
    {
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.ReadJsonAsync();
        return new Tokens(body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auth.Application.AccountsSync;
using Auth.Domain.Identities;

namespace Auth.Api.Tests;

public sealed class SignInTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Browser_gets_the_refresh_token_only_in_a_strict_http_only_cookie()
    {
        using var browser = api.Browser();

        using var registered = await AuthApiFactory.RegisterAsync(browser, AuthApiFactory.Unique("browser"));

        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var body = await registered.ReadJsonAsync();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("refreshToken").ValueKind);
        var cookie = Assert.Single(registered.Headers.GetValues("Set-Cookie"), c => c.StartsWith("ha_refresh=", StringComparison.Ordinal));
        Assert.Contains("path=/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_from_the_cookie_then_logout_ends_the_session()
    {
        using var browser = api.Browser();
        var email = AuthApiFactory.Unique("refresh");
        using var _ = await AuthApiFactory.RegisterAsync(browser, email);

        using var refreshed = await browser.PostAsJsonAsync("/auth/refresh", new { clientId = AuthApiFactory.WebClient });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var accessToken = (await refreshed.ReadJsonAsync()).GetProperty("accessToken").GetString();

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var me = await browser.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, (await me.ReadJsonAsync()).GetProperty("email").GetString());

        using var loggedOut = await browser.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);

        using var again = await browser.PostAsJsonAsync("/auth/refresh", new { clientId = AuthApiFactory.WebClient });
        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);
    }

    [Fact]
    public async Task Native_client_gets_the_refresh_token_in_the_body()
    {
        using var client = api.CreateClient();
        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("native"), AuthApiFactory.NativeClient);

        Assert.False(registered.Headers.Contains("Set-Cookie"));
        var refreshToken = (await registered.ReadJsonAsync()).GetProperty("refreshToken").GetString();
        Assert.False(string.IsNullOrEmpty(refreshToken));

        using var refreshed = await client.PostAsJsonAsync("/auth/refresh", new { clientId = AuthApiFactory.NativeClient, refreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(refreshToken, (await refreshed.ReadJsonAsync()).GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Wrong_password_is_401_with_a_code()
    {
        using var client = api.CreateClient();
        var email = AuthApiFactory.Unique("wrong");
        using var _ = await AuthApiFactory.RegisterAsync(client, email);

        using var login = await client.PostAsJsonAsync("/auth/login", new { email, password = "not the password!", clientId = AuthApiFactory.WebClient });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.False(string.IsNullOrEmpty((await login.ReadJsonAsync()).GetProperty("code").GetString()));
    }

    [Fact]
    public async Task Self_disabled_account_gets_a_ticket_and_waits_for_accounts()
    {
        using var client = api.CreateClient();
        var email = AuthApiFactory.Unique("disabled");
        using var registered = await AuthApiFactory.RegisterAsync(client, email, AuthApiFactory.NativeClient);
        var accessToken = (await registered.ReadJsonAsync()).GetProperty("accessToken").GetString();
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var me = await client.SendAsync(meRequest);
        var id = (await me.ReadJsonAsync()).GetProperty("id").GetGuid();
        Assert.True((await api.SendAsync(new ApplyAccountStatusCommand(id, AccountStatus.DisabledBySelf))).IsSuccess);

        using var login = await client.PostAsJsonAsync("/auth/login", new { email, password = AuthApiFactory.Password, clientId = AuthApiFactory.NativeClient });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        var problem = await login.ReadJsonAsync();
        Assert.Equal("account.disabled_by_user", problem.GetProperty("code").GetString());
        var ticket = problem.GetProperty("ticket").GetString();

        using var complete = await client.PostAsJsonAsync("/auth/reactivation/complete", new { ticket, clientId = AuthApiFactory.NativeClient });
        Assert.Equal(HttpStatusCode.Conflict, complete.StatusCode);
        Assert.Equal("reactivation.pending", (await complete.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal(TimeSpan.FromSeconds(2), complete.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Unknown_client_is_rejected()
    {
        using var client = api.CreateClient();

        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("client"), "nobody");

        Assert.Equal(HttpStatusCode.BadRequest, registered.StatusCode);
        Assert.Equal("client.unknown", (await registered.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Forgot_password_answers_202_even_over_the_per_address_limit()
    {
        using var client = api.CreateClient();
        var email = AuthApiFactory.Unique("forgot");

        for (var i = 0; i < 5; i++)
        {
            using var forgot = await client.PostAsJsonAsync("/auth/password/forgot", new { email });
            Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);
        }
    }
}

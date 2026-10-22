using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Tests;

public sealed class GoogleTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task Sign_in_with_google_ends_in_vue_with_a_refresh_cookie()
    {
        using var browser = api.Browser();
        api.Google.Identity = new ExternalIdentity(Guid.NewGuid().ToString("N"), AuthApiFactory.Unique("google"), true, "Ewa", null, null);

        using var start = await browser.GetAsync($"/auth/external/google?client_id={AuthApiFactory.WebClient}&returnUrl=/home");
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        var google = start.Headers.Location!;
        Assert.StartsWith(AuthApiFactory.GoogleAuthorize.ToString(), google.ToString(), StringComparison.Ordinal);
        var query = QueryHelpers.ParseQuery(google.Query);
        Assert.Equal("S256", query["code_challenge_method"]);

        using var callback = await browser.GetAsync($"/auth/external/google/callback?code=abc&state={query["state"]}");

        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal(new Uri("https://app.localhost/home"), callback.Headers.Location);
        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(api.Google.LastCodeVerifier!)));
        Assert.Equal(query["code_challenge"].ToString(), challenge);
        Assert.Equal(query["redirect_uri"].ToString(), api.Google.LastRedirectUri!.ToString());

        using var refreshed = await browser.PostAsJsonAsync("/auth/refresh", new { clientId = AuthApiFactory.WebClient });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    [Fact]
    public async Task Existing_email_comes_back_with_a_link_ticket_in_the_fragment()
    {
        using var browser = api.Browser();
        var email = AuthApiFactory.Unique("link");
        using var _ = await AuthApiFactory.RegisterAsync(api.CreateClient(), email);
        api.Google.Identity = new ExternalIdentity(Guid.NewGuid().ToString("N"), email, true, null, null, null);

        using var start = await browser.GetAsync($"/auth/external/google?client_id={AuthApiFactory.WebClient}");
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"];
        using var callback = await browser.GetAsync($"/auth/external/google/callback?code=abc&state={state}");

        var fragment = QueryHelpers.ParseQuery(callback.Headers.Location!.Fragment.TrimStart('#'));
        Assert.Equal("external_login.link_required", fragment["error"]);
        Assert.False(string.IsNullOrEmpty(fragment["ticket"]));
        Assert.Equal(email, fragment["email"]);
        Assert.Empty(callback.Headers.Location.Query);
    }

    [Fact]
    public async Task Callback_with_another_state_is_refused()
    {
        using var browser = api.Browser();
        using var _ = await browser.GetAsync($"/auth/external/google?client_id={AuthApiFactory.WebClient}");

        using var callback = await browser.GetAsync("/auth/external/google/callback?code=abc&state=forged");

        Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example/home")]
    [InlineData("//evil.example/home")]
    public async Task Return_url_outside_the_client_is_refused(string returnUrl)
    {
        using var browser = api.Browser();

        using var start = await browser.GetAsync(
            $"/auth/external/google?client_id={AuthApiFactory.WebClient}&returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.BadRequest, start.StatusCode);
        Assert.Equal("client.return_url_not_allowed", (await start.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Native_clients_cannot_use_google()
    {
        using var browser = api.Browser();

        using var start = await browser.GetAsync($"/auth/external/google?client_id={AuthApiFactory.NativeClient}");

        Assert.Equal("external_login.browser_only", (await start.ReadJsonAsync()).GetProperty("code").GetString());
    }
}

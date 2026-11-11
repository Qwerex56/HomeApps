using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Auth.Api.Tests;

/// <summary>jarvis-v1.md 3.1: an app (Claude Code) connects through OAuth 2.1, the way the MCP specification asks.</summary>
public sealed class ConnectedAppTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    // From appsettings.Development.json.
    private const string Resource = "https://app.localhost/jarvis/mcp";
    private const string RegisteredRedirect = "http://localhost:33418/callback";

    // Claude Code listens on a new port each time (RFC 8252).
    private const string Redirect = "http://localhost:50123/callback";

    private static readonly string[] RegisteredRedirects = [RegisteredRedirect];
    private static readonly string[] EvilRedirects = ["http://evil.example/cb"];
    private static readonly string[] GrantTypes = ["authorization_code", "refresh_token"];

    [Fact]
    public async Task Discovery_names_every_endpoint_an_mcp_client_needs()
    {
        using var client = api.CreateClient();

        var metadata = await client.GetFromJsonAsync<Dictionary<string, object>>("/.well-known/oauth-authorization-server");

        Assert.Equal("https://app.localhost/auth/oauth/authorize", metadata!["authorization_endpoint"].ToString());
        Assert.Equal("https://app.localhost/auth/oauth/token", metadata["token_endpoint"].ToString());
        Assert.Equal("https://app.localhost/auth/oauth/register", metadata["registration_endpoint"].ToString());
        Assert.Contains("S256", metadata["code_challenge_methods_supported"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_app_connects_gets_tokens_only_for_its_resource_and_shows_in_connected_apps()
    {
        using var client = api.CreateClient(new() { AllowAutoRedirect = false });
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);
        var pkce = Pkce.New();

        using var authorize = await client.GetAsync($"/auth/oauth/authorize?{Query(app, pkce.Challenge, "xyz")}");
        Assert.Equal(HttpStatusCode.Redirect, authorize.StatusCode);
        Assert.StartsWith("/connect?", authorize.Headers.Location!.OriginalString, StringComparison.Ordinal);

        using var preview = await Send(client, HttpMethod.Get, $"/auth/oauth/consent?{Query(app, pkce.Challenge, "xyz")}", person);
        var shown = await preview.ReadJsonAsync();
        Assert.Equal("Claude Code", shown.GetProperty("clientName").GetString());
        Assert.Equal("localhost", shown.GetProperty("redirectHost").GetString());

        var code = await AllowAsync(client, person, app, pkce.Challenge, "xyz");
        var tokens = await TokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code", ["client_id"] = app, ["code"] = code,
            ["redirect_uri"] = Redirect, ["code_verifier"] = pkce.Verifier, ["resource"] = Resource,
        });
        Assert.Equal(HttpStatusCode.OK, tokens.Status);

        var access = new JsonWebToken(tokens.Body.GetProperty("access_token").GetString());
        Assert.Equal([Resource], access.Audiences);
        Assert.Equal(app, access.GetPayloadValue<string>("client_id"));
        Assert.Equal("homeapps", access.GetPayloadValue<string>("scope"));
        Assert.Equal("Bearer", tokens.Body.GetProperty("token_type").GetString());

        // The token is for the gateway, never for the services themselves.
        using var me = await Send(client, HttpMethod.Get, "/auth/me", access.EncodedToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        using var apps = await Send(client, HttpMethod.Get, "/auth/me/apps", person);
        var connected = Assert.Single((await apps.ReadJsonAsync()).EnumerateArray());
        Assert.Equal("Claude Code", connected.GetProperty("name").GetString());
        using var sessions = await Send(client, HttpMethod.Get, "/auth/me/sessions", person);
        Assert.DoesNotContain((await sessions.ReadJsonAsync()).EnumerateArray(), s => s.GetProperty("clientId").GetString() == app);

        // "Disconnect" ends the app's session: its refresh token stops working.
        using var disconnect = await Send(client, HttpMethod.Delete, $"/auth/me/sessions/{connected.GetProperty("id").GetGuid()}", person);
        var refresh = await TokenAsync(client, new()
        {
            ["grant_type"] = "refresh_token", ["client_id"] = app, ["refresh_token"] = tokens.Body.GetProperty("refresh_token").GetString()!,
        });
        Assert.Equal(HttpStatusCode.BadRequest, refresh.Status);
        Assert.Equal("invalid_grant", refresh.Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Refresh_rotates_and_revoke_ends_the_session()
    {
        using var client = api.CreateClient();
        var (app, first) = await ConnectedAsync(client);

        var second = await TokenAsync(client, new()
        {
            ["grant_type"] = "refresh_token", ["client_id"] = app, ["refresh_token"] = first,
        });
        Assert.Equal(HttpStatusCode.OK, second.Status);
        var rotated = second.Body.GetProperty("refresh_token").GetString()!;
        Assert.NotEqual(first, rotated);

        using var revoke = await client.PostAsync("/auth/oauth/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = rotated, ["client_id"] = app,
        }));
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        var after = await TokenAsync(client, new() { ["grant_type"] = "refresh_token", ["client_id"] = app, ["refresh_token"] = rotated });
        Assert.Equal("invalid_grant", after.Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task A_code_used_twice_ends_the_session_it_started()
    {
        using var client = api.CreateClient();
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);
        var pkce = Pkce.New();
        var code = await AllowAsync(client, person, app, pkce.Challenge, null);
        var redeem = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = app, ["code"] = code,
            ["redirect_uri"] = Redirect, ["code_verifier"] = pkce.Verifier,
        };

        var first = await TokenAsync(client, redeem);
        var second = await TokenAsync(client, redeem);
        var refresh = await TokenAsync(client, new()
        {
            ["grant_type"] = "refresh_token", ["client_id"] = app, ["refresh_token"] = first.Body.GetProperty("refresh_token").GetString()!,
        });

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Equal("invalid_grant", second.Body.GetProperty("error").GetString());
        Assert.Equal("invalid_grant", refresh.Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task A_wrong_verifier_gets_nothing()
    {
        using var client = api.CreateClient();
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);
        var code = await AllowAsync(client, person, app, Pkce.New().Challenge, null);

        var tokens = await TokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code", ["client_id"] = app, ["code"] = code,
            ["redirect_uri"] = Redirect, ["code_verifier"] = Pkce.New().Verifier,
        });

        Assert.Equal(HttpStatusCode.BadRequest, tokens.Status);
        Assert.Equal("invalid_grant", tokens.Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Refusing_sends_the_browser_back_with_access_denied()
    {
        using var client = api.CreateClient();
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);

        using var decision = await Send(client, HttpMethod.Post, "/auth/oauth/consent", person,
            ConsentBody(app, Pkce.New().Challenge, "s1", allow: false));

        var location = (await decision.ReadJsonAsync()).GetProperty("location").GetString();
        Assert.Equal($"{Redirect}?error=access_denied&state=s1", location);
    }

    [Fact]
    public async Task An_address_the_app_did_not_register_is_shown_as_an_error_never_redirected_to()
    {
        using var client = api.CreateClient();
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);
        var query = Query(app, Pkce.New().Challenge, null).Replace(Uri.EscapeDataString(Redirect), Uri.EscapeDataString("https://evil.example/cb"), StringComparison.Ordinal);

        using var preview = await Send(client, HttpMethod.Get, $"/auth/oauth/consent?{query}", person);

        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Equal("oauth.invalid_redirect_uri", (await preview.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Registration_answers_in_oauth_shape()
    {
        using var client = api.CreateClient();

        using var refused = await client.PostAsJsonAsync("/auth/oauth/register", new { client_name = "X", redirect_uris = EvilRedirects });

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("invalid_redirect_uri", (await refused.ReadJsonAsync()).GetProperty("error").GetString());
    }

    [Fact]
    public async Task An_assistant_cannot_connect_apps_for_the_person()
    {
        using var client = api.CreateClient();
        var person = await PersonAsync(client);
        using var me = await Send(client, HttpMethod.Get, "/auth/me", person);
        var userId = (await me.ReadJsonAsync()).GetProperty("id").GetGuid();
        using var delegation = await client.PostAsJsonAsync("/internal/delegation", new
        {
            clientId = "jarvis-service", clientSecret = "development-only-jarvis-secret-0123456789", userId, channel = "test",
        });
        var assistant = (await delegation.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
        var app = await RegisterAppAsync(client);

        using var decision = await Send(client, HttpMethod.Post, "/auth/oauth/consent", assistant, ConsentBody(app, Pkce.New().Challenge, null, allow: true));

        Assert.Equal(HttpStatusCode.Forbidden, decision.StatusCode);
    }

    private static async Task<(string App, string RefreshToken)> ConnectedAsync(HttpClient client)
    {
        var person = await PersonAsync(client);
        var app = await RegisterAppAsync(client);
        var pkce = Pkce.New();
        var code = await AllowAsync(client, person, app, pkce.Challenge, null);
        var tokens = await TokenAsync(client, new()
        {
            ["grant_type"] = "authorization_code", ["client_id"] = app, ["code"] = code,
            ["redirect_uri"] = Redirect, ["code_verifier"] = pkce.Verifier,
        });
        return (app, tokens.Body.GetProperty("refresh_token").GetString()!);
    }

    private static async Task<string> PersonAsync(HttpClient client)
    {
        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("apps"), AuthApiFactory.NativeClient);
        return (await registered.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
    }

    private static async Task<string> RegisterAppAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/auth/oauth/register", new
        {
            client_name = "Claude Code",
            redirect_uris = RegisteredRedirects,
            grant_types = GrantTypes,
            token_endpoint_auth_method = "none",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("client_id").GetString()!;
    }

    private static async Task<string> AllowAsync(HttpClient client, string person, string app, string challenge, string? state)
    {
        using var decision = await Send(client, HttpMethod.Post, "/auth/oauth/consent", person, ConsentBody(app, challenge, state, allow: true));
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        var location = new Uri((await decision.ReadJsonAsync()).GetProperty("location").GetString()!);
        Assert.StartsWith(Redirect, location.ToString(), StringComparison.Ordinal);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal(state, query.TryGetValue("state", out var s) ? s.ToString() : null);
        return query["code"].ToString();
    }

    private static object ConsentBody(string app, string challenge, string? state, bool allow) => new
    {
        responseType = "code", clientId = app, redirectUri = Redirect, codeChallenge = challenge,
        codeChallengeMethod = "S256", resource = Resource, state, allow,
    };

    private static string Query(string app, string challenge, string? state) =>
        $"response_type=code&client_id={app}&redirect_uri={Uri.EscapeDataString(Redirect)}&code_challenge={challenge}"
        + $"&code_challenge_method=S256&resource={Uri.EscapeDataString(Resource)}" + (state is null ? "" : $"&state={state}");

    private static async Task<(HttpStatusCode Status, System.Text.Json.JsonElement Body)> TokenAsync(HttpClient client, Dictionary<string, string> form)
    {
        using var response = await client.PostAsync("/auth/oauth/token", new FormUrlEncodedContent(form));
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        return (response.StatusCode, await response.ReadJsonAsync());
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private sealed record Pkce(string Verifier, string Challenge)
    {
        public static Pkce New()
        {
            var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            return new Pkce(verifier, Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
        }
    }
}

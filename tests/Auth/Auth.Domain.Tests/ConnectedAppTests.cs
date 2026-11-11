using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;

namespace Auth.Domain.Tests;

/// <summary>jarvis-v1.md 3.1: apps that act for a person through OAuth.</summary>
public sealed class ConnectedAppTests
{
    private const string Resource = "https://home.example/jarvis/mcp";
    private const string Verifier = "a-verifier-that-is-long-enough-for-pkce-0123456789";
    private static readonly string Challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier)));

    private readonly FixedTime _time = new(A.Now);

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("http://localhost:33418/callback", true)]
    [InlineData("http://127.0.0.1:5000/cb", true)]
    [InlineData("http://example.com/callback", false)]
    [InlineData("https://claude.ai/callback#fragment", false)]
    [InlineData("/callback", false)]
    [InlineData("myapp://callback", false)]
    public void Only_https_or_loopback_addresses_may_receive_a_code(string uri, bool acceptable) =>
        Assert.Equal(acceptable, RedirectUri.IsAcceptable(uri));

    [Fact]
    public void A_loopback_address_may_come_back_on_another_port_anything_else_must_match_exactly()
    {
        Assert.True(RedirectUri.Matches("http://localhost:33418/callback", "http://localhost:51234/callback"));
        Assert.False(RedirectUri.Matches("http://localhost:33418/callback", "http://localhost:51234/other"));
        Assert.False(RedirectUri.Matches("https://claude.ai/cb", "https://claude.ai/cb/"));
        Assert.False(RedirectUri.Matches("https://claude.ai:443/cb", "https://claude.ai:8443/cb"));
    }

    [Fact]
    public void Pkce_accepts_only_the_verifier_behind_the_challenge()
    {
        Assert.True(Pkce.Verify(Verifier, Challenge));
        Assert.False(Pkce.Verify(Verifier + "x", Challenge));
        Assert.False(Pkce.Verify("short", Challenge));
        Assert.True(Pkce.IsValidChallenge(Challenge));
        Assert.False(Pkce.IsValidChallenge("plain-text-challenge"));
    }

    [Fact]
    public void An_app_registers_with_a_name_and_redirect_uris_and_gets_a_client_id()
    {
        var client = AppClient.Register(" Claude Code ", ["http://localhost:33418/callback"], A.Now);

        Assert.True(client.IsSuccess);
        Assert.StartsWith(AppClient.IdPrefix, client.Value.Id, StringComparison.Ordinal);
        Assert.Equal("Claude Code", client.Value.Name);
        Assert.True(client.Value.AllowsRedirect("http://localhost:40000/callback"));
        Assert.False(client.Value.AllowsRedirect("https://evil.example/callback"));
    }

    [Theory]
    [InlineData("", "https://claude.ai/cb", "oauth.invalid_client_metadata")]
    [InlineData("Claude", "http://evil.example/cb", "oauth.invalid_redirect_uri")]
    public void An_app_without_a_name_or_with_a_bad_address_is_refused(string name, string uri, string code) =>
        Assert.Equal(code, AppClient.Register(name, [uri], A.Now).Code());

    [Fact]
    public void A_code_works_once_for_the_same_app_address_and_verifier()
    {
        var (code, _) = Issue();

        Assert.Equal("oauth.invalid_grant", code.Redeem("app_other", "http://localhost/cb", Verifier, A.Now).Code());
        Assert.Equal("oauth.invalid_grant", code.Redeem("app_1", "http://localhost/other", Verifier, A.Now).Code());
        Assert.Equal("oauth.invalid_grant", code.Redeem("app_1", "http://localhost/cb", Verifier + "x", A.Now).Code());
        Assert.True(code.Redeem("app_1", "http://localhost/cb", Verifier, A.Now).IsSuccess);
        Assert.Equal("The code was already used.", code.Redeem("app_1", "http://localhost/cb", Verifier, A.Now).Errors[0].Message);
    }

    [Fact]
    public void A_code_lives_one_minute()
    {
        var (code, _) = Issue();

        Assert.Equal("oauth.invalid_grant", code.Redeem("app_1", "http://localhost/cb", Verifier, A.Now + AuthorizationCode.Lifetime).Code());
    }

    [Fact]
    public void A_connected_app_gets_its_own_session_for_one_resource_outside_my_device_sessions()
    {
        var identity = A.Registered(_time).Identity;
        var app = ClientInfo.Create("app_1", "claude-code/2.0").Value;

        var token = identity.ConnectApp(app, Resource, _time);
        var refreshed = identity.Refresh(token.Value.Raw, app, _time);

        Assert.Equal(Resource, token.Value.Audience);
        Assert.Equal(Resource, refreshed.Value.Audience);
        var session = Assert.Single(identity.ConnectedApps(A.Now));
        Assert.Equal(AuthMethod.ConnectedApp, session.Method);
        Assert.DoesNotContain(identity.DeviceSessions(A.Now), s => s.Id == session.Id);
    }

    [Fact]
    public void Signing_out_everywhere_disconnects_the_apps_too()
    {
        var identity = A.Registered(_time).Identity;
        identity.ConnectApp(ClientInfo.Create("app_1", null).Value, Resource, _time);

        identity.SignOutEverywhere(_time);

        Assert.Empty(identity.ConnectedApps(A.Now));
    }

    private static (AuthorizationCode Code, string Raw) Issue() =>
        AuthorizationCode.Issue("app_1", Guid.NewGuid(), "http://localhost/cb", Challenge, Resource, A.Now);
}

using System.Security.Cryptography;
using Auth.Application.Abstractions;
using Auth.Domain.Identities;
using Auth.Infrastructure.Security;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Tests;

internal sealed class TestEnvironment(string environmentName, string contentRoot) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "Auth.Tests";

    public string ContentRootPath { get; set; } = contentRoot;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public sealed class SecurityTests : IDisposable
{
    private const string Issuer = "https://auth.homeapps.test";

    private static readonly RegisteredClient Web = new("homeapps-web", "HomeApps", ClientType.Browser, [], ["homeapps"]);

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"auth-keys-{Guid.NewGuid():N}");
    private readonly FixedTime _time = new(A.Now);
    private readonly SigningKeys _keys;
    private readonly TokenIssuer _issuer;

    public SecurityTests()
    {
        _keys = Keys(new SigningOptions { Issuer = Issuer });
        _issuer = new TokenIssuer(_keys, Options.Create(new SigningOptions { Issuer = Issuer }), _time);
    }

    public void Dispose()
    {
        _keys.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private SigningKeys Keys(SigningOptions options, string environment = "Development") =>
        new(Options.Create(options), new TestEnvironment(environment, _root));

    [Fact]
    public void Password_hashes_verify_and_bad_hashes_are_just_false()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash(A.Password);

        Assert.True(hasher.Verify(hash, A.Password));
        Assert.False(hasher.Verify(hash, "something else"));
        Assert.False(hasher.Verify(string.Empty, A.Password));
        Assert.False(hasher.Verify("not a hash", A.Password));
    }

    [Fact]
    public async Task Access_token_carries_the_claims_other_services_read()
    {
        var registration = A.Registered(_time, "adam@example.com");

        var token = _issuer.IssueAccessToken(registration.Identity, registration.RefreshToken, Web);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = "homeapps",
            IssuerSigningKeys = _keys.Jwks.Keys,
            ValidateLifetime = false,
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal("at+jwt", jwt.Typ);
        Assert.Equal(registration.Identity.Id.ToString(), jwt.Subject);
        Assert.Equal("User", jwt.GetClaim("role").Value);
        Assert.Equal("homeapps-web", jwt.GetClaim("client_id").Value);
        Assert.Equal(A.Now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), jwt.GetClaim("auth_time").Value);
        Assert.Equal(A.Now + TokenIssuer.AccessTokenLifetime, token.ExpiresAt);
    }

    [Fact]
    public async Task Tickets_round_trip_and_expire_after_ten_minutes()
    {
        var reactivation = new ReactivationTicket(Guid.NewGuid(), AuthMethod.Google);
        var link = new ExternalLinkTicket(Guid.NewGuid(), ExternalProvider.Google, "google-1", "ola@example.com");

        var reactivationValue = _issuer.IssueTicket(reactivation);
        Assert.Equal(reactivation, await _issuer.ReadTicketAsync(reactivationValue));
        Assert.Equal(link, await _issuer.ReadTicketAsync(_issuer.IssueTicket(link)));

        _time.Now += TokenIssuer.TicketLifetime + TimeSpan.FromMinutes(1);
        Assert.Null(await _issuer.ReadTicketAsync(reactivationValue));
    }

    [Fact]
    public async Task Forged_tampered_or_access_tokens_are_not_tickets()
    {
        var registration = A.Registered(_time, "adam@example.com");
        var accessToken = _issuer.IssueAccessToken(registration.Identity, registration.RefreshToken, Web).Value;
        var ticket = _issuer.IssueTicket(new ReactivationTicket(Guid.NewGuid(), AuthMethod.Password));

        using var otherKeys = new SigningKeys(
            Options.Create(new SigningOptions { Issuer = Issuer, PrivateKeyPem = NewPem() }), new TestEnvironment("Production", _root));
        var forged = new TokenIssuer(otherKeys, Options.Create(new SigningOptions { Issuer = Issuer }), _time)
            .IssueTicket(new ReactivationTicket(Guid.NewGuid(), AuthMethod.Password));

        Assert.Null(await _issuer.ReadTicketAsync(accessToken));
        Assert.Null(await _issuer.ReadTicketAsync(forged));
        Assert.Null(await _issuer.ReadTicketAsync(ticket[..^4] + "AAAA"));
        Assert.Null(await _issuer.ReadTicketAsync("garbage"));
    }

    [Fact]
    public void Development_key_is_generated_once_and_reused()
    {
        using var again = Keys(new SigningOptions { Issuer = Issuer });

        Assert.Equal(_keys.CurrentKey.KeyId, again.CurrentKey.KeyId);
        Assert.True(File.Exists(Path.Combine(_root, ".keys", "auth-signing.pem")));
    }

    [Fact]
    public void Production_without_a_key_does_not_start()
    {
        Assert.Throws<InvalidOperationException>(() => Keys(new SigningOptions { Issuer = Issuer }, "Production"));
    }

    [Fact]
    public void Previous_key_stays_published_while_keys_change()
    {
        using var old = RSA.Create(2048);
        using var keys = Keys(new SigningOptions { Issuer = Issuer, PreviousPublicKeyPem = old.ExportSubjectPublicKeyInfoPem() });

        Assert.Equal(2, keys.Jwks.Keys.Count);
        Assert.All(keys.Jwks.Keys, k => Assert.False(k.HasPrivateKey));
        Assert.Equal(keys.CurrentKey.KeyId, keys.Jwks.Keys[0].Kid);
    }

    [Fact]
    public void Clients_without_audiences_get_the_default_one()
    {
        var options = new ClientsOptions();
        options.Clients.Add(new ClientOptions { ClientId = "jarvis", Name = "JARVIS", Type = ClientType.Native });

        var client = new ClientRegistry(Options.Create(options)).Find("jarvis");

        Assert.Equal(["homeapps"], client!.Audiences);
        Assert.Null(new ClientRegistry(Options.Create(options)).Find("JARVIS"));
    }

    private static string NewPem()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportPkcs8PrivateKeyPem();
    }
}

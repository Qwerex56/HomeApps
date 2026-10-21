using System.Net;
using System.Security.Cryptography;
using Auth.Domain.Identities;
using Auth.Infrastructure.External;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Tests;

public sealed class GoogleTests : IDisposable
{
    private const string ClientId = "client.apps.googleusercontent.com";

    private readonly RSA _googleKey = RSA.Create(2048);
    private readonly FixedTime _time = new(DateTimeOffset.UtcNow);

    public void Dispose() => _googleKey.Dispose();

    private string IdToken(string audience = ClientId, object? emailVerified = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "https://accounts.google.com",
            Audience = audience,
            Expires = _time.GetUtcNow().AddMinutes(5).UtcDateTime,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_googleKey) { KeyId = "g1" }, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = "google-123",
                ["email"] = "ewa@gmail.com",
                ["email_verified"] = emailVerified ?? true,
                ["given_name"] = "Ewa",
                ["family_name"] = "Kowalska",
                ["picture"] = "https://lh3.googleusercontent.com/a/photo",
            },
        });

    private ExternalIdentityVerifier Verifier(string idToken, HttpStatusCode status = HttpStatusCode.OK, GoogleOptions? options = null)
    {
        var metadata = new OpenIdConnectConfiguration();
        metadata.SigningKeys.Add(new RsaSecurityKey(_googleKey.ExportParameters(false)) { KeyId = "g1" });
        var http = new HttpClient(new StubHandler(status, $$"""{"id_token":"{{idToken}}"}"""));
        return new ExternalIdentityVerifier(
            http,
            new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata),
            Options.Create(options ?? new GoogleOptions { ClientId = ClientId, ClientSecret = "secret" }),
            _time,
            NullLogger<ExternalIdentityVerifier>.Instance);
    }

    private static Task<FluentResults.Result<Application.Abstractions.ExternalIdentity>> Verify(ExternalIdentityVerifier verifier,
        ExternalProvider provider = ExternalProvider.Google) =>
        verifier.VerifyAsync(provider, "code", "verifier", new Uri("https://home.test/auth/external/google/callback"), default);

    [Fact]
    public async Task Valid_id_token_gives_the_person()
    {
        var person = (await Verify(Verifier(IdToken()))).Value;

        Assert.Equal("google-123", person.Subject);
        Assert.Equal("ewa@gmail.com", person.Email);
        Assert.True(person.EmailVerified);
        Assert.Equal("Kowalska", person.LastName);
        Assert.NotNull(person.AvatarUrl);
    }

    [Fact]
    public async Task Email_verified_as_text_is_read_too()
    {
        Assert.False((await Verify(Verifier(IdToken(emailVerified: "false")))).Value.EmailVerified);
        Assert.True((await Verify(Verifier(IdToken(emailVerified: "true")))).Value.EmailVerified);
    }

    [Fact]
    public async Task Token_for_another_app_or_a_refused_code_fails()
    {
        Assert.True((await Verify(Verifier(IdToken(audience: "someone-else")))).IsFailed);
        Assert.True((await Verify(Verifier(IdToken(), HttpStatusCode.BadRequest))).IsFailed);
    }

    [Fact]
    public async Task Apple_and_unconfigured_google_are_disabled()
    {
        Assert.Equal("external_login.provider_disabled",
            (await Verify(Verifier(IdToken()), ExternalProvider.Apple)).Errors[0].Metadata["Code"]);
        Assert.True((await Verify(Verifier(IdToken(), options: new GoogleOptions()))).IsFailed);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
    }
}

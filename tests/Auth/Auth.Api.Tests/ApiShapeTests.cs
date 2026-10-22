using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Tests;

public sealed class ApiShapeTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public void Only_the_listed_endpoints_are_anonymous()
    {
        using var _ = api.CreateClient();
        var anonymous = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"])
                .Select(m => $"{m} /{e.RoutePattern.RawText!.TrimStart('/')}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] expected =
        [
            "GET /.well-known/jwks.json",
            "GET /.well-known/oauth-authorization-server",
            "GET /.well-known/openid-configuration",
            "GET /auth/external/{provider}",
            "GET /auth/external/{provider}/callback",
            "GET /auth/invitations/{token}",
            "* /health/live",
            "* /health/ready",
            "GET /openapi/{documentName}.json",
            "POST /auth/external/link/confirm",
            "POST /auth/external/link/decline",
            "POST /auth/invitations/{token}/accept",
            "POST /auth/login",
            "POST /auth/logout",
            "POST /auth/me/email/confirm",
            "POST /auth/me/password/confirm",
            "POST /auth/password/forgot",
            "POST /auth/password/reset",
            "POST /auth/reactivation/complete",
            "POST /auth/refresh",
            "POST /auth/register",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), anonymous);
    }

    [Fact]
    public async Task Everything_else_needs_a_token()
    {
        using var client = api.CreateClient();

        using var me = await client.GetAsync("/auth/me");
        using var invite = await client.PostAsJsonAsync("/auth/invitations", new { email = "x@example.com" });

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, invite.StatusCode);
    }

    [Fact]
    public async Task Tokens_validate_against_the_published_keys()
    {
        using var client = api.CreateClient();
        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("jwks"));
        var accessToken = (await registered.ReadJsonAsync()).GetProperty("accessToken").GetString();

        var metadata = await client.GetFromJsonAsync<Dictionary<string, object>>("/.well-known/openid-configuration");
        var jwksUri = new Uri(metadata!["jwks_uri"].ToString()!);
        var jwks = new JsonWebKeySet(await client.GetStringAsync(jwksUri.AbsolutePath));

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = metadata["issuer"].ToString(),
            ValidAudience = "homeapps",
            IssuerSigningKeys = jwks.GetSigningKeys(),
        });

        Assert.True(validation.IsValid, validation.Exception?.Message);
    }

    [Fact]
    public async Task Unknown_invitation_needs_no_token()
    {
        using var client = api.CreateClient();

        using var preview = await client.GetAsync("/auth/invitations/does-not-exist");

        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Equal("invitation.invalid_or_expired", (await preview.ReadJsonAsync()).GetProperty("code").GetString());
    }
}

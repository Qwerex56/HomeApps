using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Auth.Api.Tests;

/// <summary>jarvis-satellites.md 3a (S13): a household satellite's token names its household and changes nothing in Auth.</summary>
public sealed class DeviceTokenTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    // The service client from appsettings.Development.json.
    private const string DevicesClient = "devices-service";
    private const string DevicesSecret = "development-only-devices-secret-0123456789";

    [Fact]
    public async Task A_paired_satellite_gets_a_member_token_of_its_household_without_an_email()
    {
        using var client = api.CreateClient();
        var deviceId = Guid.NewGuid();
        var householdId = Guid.NewGuid();

        using var paired = await client.PostAsJsonAsync("/internal/devices",
            new { clientId = DevicesClient, clientSecret = DevicesSecret, deviceId, householdId, name = "salon" });
        var secret = (await paired.ReadJsonAsync()).GetProperty("secret").GetString();
        using var issued = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret });
        var token = (await issued.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/invitations") { Content = JsonContent.Create(new { email = "x@example.com" }) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var invitation = await client.SendAsync(request);

        var jwt = new JsonWebToken(token);
        Assert.Equal(deviceId.ToString(), jwt.Subject);
        Assert.Equal(householdId.ToString(), jwt.GetClaim("household_id").Value);
        Assert.Equal("User", jwt.GetClaim("role").Value);
        Assert.False(jwt.TryGetClaim("email", out _));
        Assert.Equal(HttpStatusCode.Forbidden, invitation.StatusCode);
    }

    [Fact]
    public async Task A_wrong_secret_or_client_gets_nothing()
    {
        using var client = api.CreateClient();
        var deviceId = Guid.NewGuid();

        using var pairing = await client.PostAsJsonAsync("/internal/devices",
            new { clientId = "jarvis-service", clientSecret = "development-only-jarvis-secret-0123456789", deviceId, householdId = Guid.NewGuid(), name = "salon" });
        using var token = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = "guess" });

        Assert.Equal(HttpStatusCode.Unauthorized, pairing.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, token.StatusCode);
    }
}

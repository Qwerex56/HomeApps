using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Auth.Api.Tests;

/// <summary>jarvis-satellites.md S4: a person sets a PIN, and JARVIS asks Auth whose PIN a satellite heard.</summary>
public sealed class SpokenPinTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    // The service clients from appsettings.Development.json.
    private const string DevicesSecret = "development-only-devices-secret-0123456789";
    private const string JarvisSecret = "development-only-jarvis-secret-0123456789";

    [Fact]
    public async Task A_satellite_hearing_the_PIN_learns_who_speaks()
    {
        using var client = api.CreateClient();
        using var registered = await AuthApiFactory.RegisterAsync(client, AuthApiFactory.Unique("pin"), AuthApiFactory.NativeClient);
        var token = (await registered.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
        using var set = await Send(client, HttpMethod.Put, "/auth/me/pin", token, new { pin = "4321", currentPassword = AuthApiFactory.Password });
        using var me = await Send(client, HttpMethod.Get, "/auth/me", token);
        var meJson = await me.ReadJsonAsync();
        var userId = meJson.GetProperty("id").GetGuid();
        var deviceId = Guid.NewGuid();
        using var paired = await client.PostAsJsonAsync("/internal/devices",
            new { clientId = "devices-service", clientSecret = DevicesSecret, deviceId, householdId = Guid.NewGuid(), name = "salon" });

        using var heard = await client.PostAsJsonAsync("/internal/devices/speaker",
            new { clientId = "jarvis-service", clientSecret = JarvisSecret, deviceId, candidates = new[] { userId }, pin = "4321" });
        using var missed = await client.PostAsJsonAsync("/internal/devices/speaker",
            new { clientId = "jarvis-service", clientSecret = JarvisSecret, deviceId, candidates = new[] { userId }, pin = "1111" });

        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        Assert.True(meJson.GetProperty("hasPin").GetBoolean());
        Assert.Equal(userId, (await heard.ReadJsonAsync()).GetProperty("userId").GetGuid());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await missed.ReadJsonAsync()).GetProperty("userId").ValueKind);
    }

    [Fact]
    public async Task Without_the_service_secret_nobody_learns_anything()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/internal/devices/speaker",
            new { clientId = "jarvis-service", clientSecret = "guess", deviceId = Guid.NewGuid(), candidates = new[] { Guid.NewGuid() }, pin = "1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
}

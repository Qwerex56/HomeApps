using System.Net;
using System.Net.Http.Json;

namespace Auth.Api.Tests;

/// <summary>H-4c item 2 and item 3 over HTTP: the device token endpoint has its own limit, and says <c>device_unpaired</c> only to the holder of the secret.</summary>
public sealed class DeviceTokenRateLimitTests(DeviceTokenRateLimitFactory api) : IClassFixture<DeviceTokenRateLimitFactory>
{
    [Fact]
    public async Task Device_token_requests_over_the_limit_get_429_with_retry_after_and_do_not_use_up_the_refresh_bucket()
    {
        using var client = api.CreateClient();
        var token = new { deviceId = Guid.NewGuid(), secret = "guess" };

        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 4; i++)
        {
            last?.Dispose();
            last = await client.PostAsJsonAsync("/auth/devices/token", token);
            statuses.Add(last.StatusCode);
        }

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
        Assert.NotNull(last!.Headers.RetryAfter);
        Assert.Equal("rate_limited", (await last.ReadJsonAsync()).GetProperty("code").GetString());
        last.Dispose();

        // The refresh bucket is untouched by the device requests above (it would be 429 already if they shared it).
        using var refresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "not-a-token", clientId = AuthApiFactory.WebClient });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}

public sealed class DeviceUnpairedTests(AuthApiFactory api) : IClassFixture<AuthApiFactory>
{
    private const string DevicesClient = "devices-service";
    private const string DevicesSecret = "development-only-devices-secret-0123456789";

    [Fact]
    public async Task The_holder_of_the_right_secret_of_an_unpaired_device_gets_401_device_unpaired_and_the_old_secret_invalid_credentials()
    {
        using var client = api.CreateClient();
        var deviceId = Guid.NewGuid();

        var first = await PairAsync(client, deviceId);
        using (var active = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first }))
        {
            Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        }

        await api.SendAsync(new Auth.Application.DevicesSync.UnpairDevice.UnpairDeviceCommand(deviceId));

        using var unpaired = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first });
        using var wrong = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = "guess" });
        var second = await PairAsync(client, deviceId);
        using var old = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = first });
        using var fresh = await client.PostAsJsonAsync("/auth/devices/token", new { deviceId, secret = second });

        Assert.Equal(HttpStatusCode.Unauthorized, unpaired.StatusCode);
        Assert.Equal("devices.device_unpaired", (await unpaired.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal("devices.invalid_credentials", (await wrong.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal("devices.invalid_credentials", (await old.ReadJsonAsync()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    private static async Task<string> PairAsync(HttpClient client, Guid deviceId)
    {
        using var paired = await client.PostAsJsonAsync("/internal/devices",
            new { clientId = DevicesClient, clientSecret = DevicesSecret, deviceId, householdId = Guid.NewGuid(), name = "salon" });
        return (await paired.ReadJsonAsync()).GetProperty("secret").GetString()!;
    }
}

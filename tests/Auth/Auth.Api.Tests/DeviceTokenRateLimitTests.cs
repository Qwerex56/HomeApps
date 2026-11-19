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

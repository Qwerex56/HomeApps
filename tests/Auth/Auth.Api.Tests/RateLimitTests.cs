using System.Net;
using System.Net.Http.Json;
using Auth.Api.Http;
using Microsoft.Extensions.Options;

namespace Auth.Api.Tests;

public sealed class LowLimitFactory : AuthApiFactory
{
    protected override int PerMinute => 2;
}

public sealed class RateLimitTests(LowLimitFactory api) : IClassFixture<LowLimitFactory>
{
    [Fact]
    public async Task Too_many_attempts_from_one_address_get_429_with_retry_after()
    {
        using var client = api.CreateClient();
        var body = new { email = "nobody@example.com", password = "whatever password", clientId = AuthApiFactory.WebClient };

        using var first = await client.PostAsJsonAsync("/auth/login", body);
        using var second = await client.PostAsJsonAsync("/auth/login", body);
        using var third = await client.PostAsJsonAsync("/auth/login", body);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter);
        Assert.Equal("rate_limited", (await third.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public void Per_address_limit_ignores_case()
    {
        using var limiter = new PerAddressLimiter(Options.Create(new RateLimitOptions { MailPerAddressPerHour = 2 }));

        Assert.True(limiter.TryAcquire("ola@example.com"));
        Assert.True(limiter.TryAcquire("OLA@example.com "));
        Assert.False(limiter.TryAcquire("Ola@Example.com"));
        Assert.True(limiter.TryAcquire("ewa@example.com"));
    }
}

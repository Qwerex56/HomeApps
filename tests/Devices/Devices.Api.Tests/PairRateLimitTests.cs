using System.Net;
using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>H-4c item 1: <c>POST /devices/pair</c> is limited per client address (read from X-Forwarded-For), and the report limit is its own.</summary>
public sealed class PairRateLimitTests(PairLimitFactory api) : IClassFixture<PairLimitFactory>
{
    private static HttpClient From(PairLimitFactory api, string address)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", address);
        return client;
    }

    private static Task<HttpResponseMessage> PairAsync(HttpClient client, string code) => client.PostAsJsonAsync("/devices/pair", new { code });

    [Fact]
    public async Task Too_many_pairing_attempts_from_one_address_get_429_with_retry_after_and_another_address_has_its_own_bucket()
    {
        using var one = From(api, "203.0.113.10");
        using var other = From(api, "203.0.113.11");

        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? last = null;
        for (var i = 0; i < PairLimitFactory.PairPerMinute + 1; i++)
        {
            last?.Dispose();
            last = await PairAsync(one, "ABCD2345");
            statuses.Add(last.StatusCode);
        }

        Assert.All(statuses.Take(PairLimitFactory.PairPerMinute), s => Assert.Equal(HttpStatusCode.NotFound, s));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
        Assert.NotNull(last!.Headers.RetryAfter);
        Assert.Equal("rate_limited", (await last.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("code").GetString());
        last.Dispose();

        using var untouched = await PairAsync(other, "ABCD2345");
        Assert.Equal(HttpStatusCode.NotFound, untouched.StatusCode);
    }

    [Fact]
    public async Task A_right_code_pairs_within_the_limit()
    {
        var household = Guid.NewGuid();
        var admin = Guid.NewGuid();
        api.Members.Add(admin, household, HouseholdRole.Admin);
        using var adminClient = api.CreateAuthenticatedClient(admin);
        var added = await adminClient.PostAsJsonAsync($"/devices/households/{household}", new { name = "salon" });
        var code = (await added.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("code").GetString()!;

        using var client = From(api, "203.0.113.20");
        using var paired = await PairAsync(client, code);

        Assert.Equal(HttpStatusCode.OK, paired.StatusCode);
    }

    [Fact]
    public async Task Reports_keep_their_own_limit_of_60_a_minute_per_device_token_whatever_the_pairing_bucket_says()
    {
        var household = Guid.NewGuid();
        var device = Guid.NewGuid();
        using var exhausted = From(api, "203.0.113.30");
        for (var i = 0; i < PairLimitFactory.PairPerMinute + 1; i++)
        {
            (await PairAsync(exhausted, "ABCD2345")).Dispose();
        }

        using var reporting = api.CreateClient();
        reporting.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.30");
        reporting.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateDeviceToken(device, household));
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 61; i++)
        {
            using var response = await reporting.PostAsJsonAsync("/devices/report", new[] { new { type = "status" } });
            statuses.Add(response.StatusCode);
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(60));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[60]);
    }
}

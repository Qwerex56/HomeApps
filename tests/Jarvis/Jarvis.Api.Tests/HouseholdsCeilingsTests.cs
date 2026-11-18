using System.Net;
using System.Text;
using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Tests;

/// <summary>H-2: Households' assistant view is kept 45 seconds; a 404 counts as closed and is kept, a failure closes without being kept.</summary>
public sealed class HouseholdsCeilingsTests
{
    private static readonly Guid Household = Guid.NewGuid();

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HouseholdsCeilings Ceilings(Handler handler) => new(
        new HttpClient(handler),
        Options.Create(new GatewayOptions { HouseholdsUrl = "http://households:8080/", ClientId = "jarvis-service", ClientSecret = "s3cret" }),
        new MemoryCache(new MemoryCacheOptions()),
        NullLogger<HouseholdsCeilings>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task A_found_household_is_read_with_the_service_secret_and_kept()
    {
        var handler = new Handler(_ => Json("""{"enabled":true,"accessLevel":"Member","toolSources":"Plugins, Devices"}"""));
        var ceilings = Ceilings(handler);

        var first = await ceilings.GetAsync(Household, CancellationToken.None);
        var second = await ceilings.GetAsync(Household, CancellationToken.None);

        Assert.True(first.Enabled);
        Assert.False(first.ReadOnly);
        Assert.True(first.Plugins);
        Assert.True(first.Devices);
        Assert.False(first.HomeMix);
        Assert.Same(first, second);
        var request = Assert.Single(handler.Requests);
        Assert.Equal($"http://households:8080/internal/households/{Household}/assistant", request.RequestUri!.ToString());
        Assert.Equal("jarvis-service", request.Headers.GetValues("X-Client-Id").Single());
        Assert.Equal("s3cret", request.Headers.GetValues("X-Client-Secret").Single());
    }

    [Fact]
    public async Task A_missing_household_is_closed_and_the_404_is_kept()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var ceilings = Ceilings(handler);

        Assert.Same(HouseholdCeiling.Closed, await ceilings.GetAsync(Household, CancellationToken.None));
        Assert.Same(HouseholdCeiling.Closed, await ceilings.GetAsync(Household, CancellationToken.None));
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_failure_closes_the_household_but_is_not_kept(HttpStatusCode status)
    {
        var handler = new Handler(_ => new HttpResponseMessage(status));
        var ceilings = Ceilings(handler);

        Assert.Same(HouseholdCeiling.Closed, await ceilings.GetAsync(Household, CancellationToken.None));
        Assert.Same(HouseholdCeiling.Closed, await ceilings.GetAsync(Household, CancellationToken.None));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task A_network_error_closes_the_household_and_the_next_call_asks_again()
    {
        var calls = 0;
        var handler = new Handler(_ => ++calls == 1
            ? throw new HttpRequestException("down")
            : Json("""{"enabled":true,"accessLevel":"Member","toolSources":"HomeMix"}"""));
        var ceilings = Ceilings(handler);

        Assert.Same(HouseholdCeiling.Closed, await ceilings.GetAsync(Household, CancellationToken.None));
        Assert.True((await ceilings.GetAsync(Household, CancellationToken.None)).HomeMix);
    }
}

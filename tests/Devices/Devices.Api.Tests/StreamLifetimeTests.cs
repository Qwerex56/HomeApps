using System.Net;
using HomeApps.Contracts.Households;

namespace Devices.Api.Tests;

/// <summary>The household stream (SSE) ends cleanly, not with an aborted connection, when its lifetime runs out.</summary>
public sealed class StreamLifetimeTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    [Fact]
    public async Task The_stream_completes_normally_when_its_lifetime_ends_while_the_client_is_connected()
    {
        var household = Guid.NewGuid();
        var member = Guid.NewGuid();
        api.Members.Add(member, household, HouseholdRole.Member);
        using var client = api.CreateAuthenticatedClient(member);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        using var response = await client.GetAsync($"/devices/households/{household}/stream", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        var body = await response.Content.ReadAsStringAsync(timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("ping", body, StringComparison.Ordinal);
    }
}

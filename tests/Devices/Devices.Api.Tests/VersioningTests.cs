using System.Net;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Api.Tests;

/// <summary>Release 3.0: the new path and the deprecated one answer alike; the old one says it is going away.</summary>
public sealed class VersioningTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    [Fact]
    public async Task The_legacy_path_answers_with_deprecation_headers_and_the_v1_path_without()
    {
        using var client = api.CreateAuthenticatedClient(Guid.NewGuid());

        using var current = await client.GetAsync(new Uri("/devices/v1", UriKind.Relative));
        using var legacy = await client.GetAsync(new Uri("/devices", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        Assert.False(current.Headers.Contains("Deprecation"));
        Assert.Equal("true", Assert.Single(legacy.Headers.GetValues("Deprecation")));
        Assert.Equal("Sat, 01 Jan 2028 00:00:00 GMT", Assert.Single(legacy.Headers.GetValues("Sunset")));
    }

    [Fact]
    public void The_sockets_stay_without_a_version()
    {
        using var _ = api.CreateClient();
        var patterns = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>().Select(e => e.RoutePattern.RawText).ToList();

        Assert.Contains("/devices/link", patterns);
        Assert.DoesNotContain("/devices/v1/link", patterns);
    }
}

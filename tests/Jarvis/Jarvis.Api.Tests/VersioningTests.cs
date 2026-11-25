using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Api.Tests;

public sealed class VersioningTests(JarvisFactory api) : IClassFixture<JarvisFactory>
{
    [Fact]
    public void The_voice_socket_stays_without_a_version()
    {
        using var _ = api.CreateClient();
        var patterns = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>().Select(e => e.RoutePattern.RawText).ToList();

        Assert.Contains("/jarvis/voice", patterns);
        Assert.DoesNotContain("/jarvis/v1/voice", patterns);
    }
}

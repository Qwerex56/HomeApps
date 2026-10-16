using System.Net;

namespace ServiceName.IntegrationTests;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class HealthTests(ServiceNameApiFactory factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_are_anonymous_and_healthy(string path)
    {
        var response = await factory.CreateClient().GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

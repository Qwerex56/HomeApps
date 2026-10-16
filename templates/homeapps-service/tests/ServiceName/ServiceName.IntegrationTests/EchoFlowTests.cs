using System.Net;
using System.Net.Http.Json;
using MassTransit.Testing;
using ServiceName.Application.Echoes.GetEcho;
using Contracts = HomeApps.Contracts.ServiceName;

namespace ServiceName.IntegrationTests;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class EchoFlowTests(ServiceNameApiFactory factory)
{
    [Fact]
    public async Task Creating_an_echo_publishes_through_the_outbox_and_the_consumer_marks_it_delivered()
    {
        var userId = Guid.NewGuid();
        var client = factory.CreateAuthenticatedClient(userId);

        var created = await client.PostAsJsonAsync("/echoes", new { message = "hello" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = await created.Content.ReadFromJsonAsync<Guid>();

        Assert.True(await factory.Harness.Consumed.Any<Contracts.EchoCreated>(m => m.Context.Message.EchoId == id));

        EchoResponse? echo = null;
        for (var attempt = 0; attempt < 50 && echo?.DeliveredAt is null; attempt++)
        {
            await Task.Delay(100);
            echo = await client.GetFromJsonAsync<EchoResponse>(new Uri($"/echoes/{id}", UriKind.Relative));
        }

        Assert.Equal("hello", echo!.Message);
        Assert.NotNull(echo.DeliveredAt);
    }

    [Fact]
    public async Task Invalid_input_returns_a_validation_problem()
    {
        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/echoes", new { message = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Someone_else_cannot_read_my_echo()
    {
        var mine = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var created = await mine.PostAsJsonAsync("/echoes", new { message = "secret" });
        var id = await created.Content.ReadFromJsonAsync<Guid>();

        var response = await factory.CreateAuthenticatedClient(Guid.NewGuid()).GetAsync(new Uri($"/echoes/{id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_require_a_token()
    {
        var response = await factory.CreateClient().GetAsync(new Uri($"/echoes/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_echo_returns_404()
    {
        var response = await factory.CreateAuthenticatedClient(Guid.NewGuid())
            .GetAsync(new Uri($"/echoes/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

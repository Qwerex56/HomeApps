using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;

namespace HomeApps.Plugins.Tests;

/// <summary>The realtime hub's token in the query (browsers cannot send headers with a WebSocket), and only there.</summary>
public sealed class RealtimeTests(HelloPluginFactory factory) : IClassFixture<HelloPluginFactory>
{
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();

    [Fact]
    public async Task The_manifest_names_the_realtime_hub()
    {
        var json = await factory.CreateClient().GetFromJsonAsync<JsonNode>("/plugins/hello/manifest");

        Assert.Equal("/plugins/hello/live", json!["endpoints"]!["realtime"]!.GetValue<string>());
    }

    [Fact]
    public async Task The_hub_takes_the_token_from_the_query_and_passes_it_on_to_Households()
    {
        factory.Households.SetRole(_household, _guest, HouseholdRole.Guest);
        var token = TestJwt.CreateToken(_guest);

        var response = await factory.CreateClient().GetAsync(Hub(token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var membership = await response.Content.ReadFromJsonAsync<JsonNode>();
        Assert.Equal("Guest", membership!["role"]!.GetValue<string>());
        Assert.Equal(FakeHouseholds.JoinedAt, membership["joinedAt"]!.GetValue<DateTimeOffset>());
        Assert.Equal(_guest.ToString(), TestUser.Of(factory.Households.LastAuthorization));
    }

    [Fact]
    public async Task The_hub_without_a_token_is_refused()
    {
        var response = await factory.CreateClient().GetAsync(new Uri($"/plugins/hello/live/households/{_household}/me", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_in_the_query_counts_nowhere_but_the_hub()
    {
        var token = TestJwt.CreateToken(_guest);

        var response = await factory.CreateClient().GetAsync(
            new Uri($"/plugins/hello/api/households/{_household}/greetings?access_token={token}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Uri Hub(string token) => new($"/plugins/hello/live/households/{_household}/me?access_token={token}", UriKind.Relative);
}

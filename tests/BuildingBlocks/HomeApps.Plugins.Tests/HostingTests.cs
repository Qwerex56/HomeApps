using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeApps.Contracts.Households;
using HomeApps.Plugins.Hello;
using HomeApps.Plugins.Hosting;
using MassTransit.Testing;

namespace HomeApps.Plugins.Tests;

public sealed class HostingTests(HelloPluginFactory factory) : IClassFixture<HelloPluginFactory>
{
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();

    [Fact]
    public async Task The_manifest_is_public_and_under_the_plugins_prefix()
    {
        var json = await factory.CreateClient().GetFromJsonAsync<JsonNode>("/plugins/hello/manifest");

        Assert.Equal("hello", json!["id"]!.GetValue<string>());
        Assert.Equal("/plugins/hello/ui/entry.js", json["ui"]!["entry"]!.GetValue<string>());
        Assert.Equal("Admin", json["enablement"]!["minRole"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("/plugins/hello/openapi/v1.json")]
    [InlineData("/plugins/hello/health/live")]
    public async Task OpenApi_and_health_need_no_token(string path)
    {
        var response = await factory.CreateClient().GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_ui_module_is_served_without_a_token_and_never_cached()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/plugins/hello/ui/entry.js", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-cache", response.Headers.CacheControl!.ToString());
        Assert.Contains("hello", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_api_needs_a_token()
    {
        var response = await factory.CreateClient().GetAsync(new Uri($"/plugins/hello/api/households/{_household}/greetings", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_plugin_starts_switched_off_and_says_so()
    {
        GivenHousehold();

        var enablement = await As(_member).GetFromJsonAsync<EnablementView>(Enablement);
        var greetings = await As(_member).GetAsync(Greetings);

        Assert.False(enablement!.Enabled);
        Assert.Equal(HttpStatusCode.NotFound, greetings.StatusCode);
        Assert.Equal("plugin.disabled", await CodeOf(greetings));
    }

    [Fact]
    public async Task Only_the_role_from_the_manifest_switches_it_on_and_the_first_time_seeds_data()
    {
        GivenHousehold();

        var byMember = await As(_member).PutAsJsonAsync(Enablement, new EnablementRequest(true));
        var byOwner = await As(_owner).PutAsJsonAsync(Enablement, new EnablementRequest(true));

        Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
        var view = await byOwner.Content.ReadFromJsonAsync<EnablementView>();
        Assert.True(view!.Enabled);
        Assert.Equal(_owner, view.ChangedBy);

        var greetings = await As(_guest).GetFromJsonAsync<List<GreetingView>>(Greetings);
        Assert.Equal("Cześć!", Assert.Single(greetings!).Text);
    }

    [Fact]
    public async Task Permissions_follow_the_household_role()
    {
        await GivenSwitchedOn();

        var byGuest = await As(_guest).PostAsJsonAsync(Greetings, new { text = "Hej" });
        var byMember = await As(_member).PostAsJsonAsync(Greetings, new { text = "Hej" });
        var byStranger = await As(Guid.NewGuid()).GetAsync(Greetings);

        Assert.Equal(HttpStatusCode.Forbidden, byGuest.StatusCode);
        Assert.Equal("plugin.forbidden", await CodeOf(byGuest));
        Assert.Equal(HttpStatusCode.OK, byMember.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, byStranger.StatusCode);
        Assert.Equal("household.not_found", await CodeOf(byStranger));
    }

    [Fact]
    public async Task Validation_still_runs_for_plugin_commands()
    {
        await GivenSwitchedOn();

        var response = await As(_member).PostAsJsonAsync(Greetings, new { text = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Households_is_asked_with_the_callers_token_and_the_answer_is_cached()
    {
        await GivenSwitchedOn();
        var client = As(_member);
        await client.GetAsync(Greetings);
        var calls = factory.Households.Calls;

        await client.GetAsync(Greetings);
        await client.GetAsync(Greetings);

        Assert.Equal(calls, factory.Households.Calls);
        Assert.Equal(_member.ToString(), TestUser.Of(factory.Households.LastAuthorization));
    }

    [Fact]
    public async Task A_role_change_from_Households_takes_effect_at_once()
    {
        await GivenSwitchedOn();
        Assert.Equal(HttpStatusCode.OK, (await As(_member).PostAsJsonAsync(Greetings, new { text = "Hej" })).StatusCode);

        factory.Households.SetRole(_household, _member, HouseholdRole.Guest);
        await factory.Harness.Bus.Publish(new MemberRoleChanged(_household, _member, HouseholdRole.Member, HouseholdRole.Guest));
        Assert.True(await factory.Harness.Consumed.Any<MemberRoleChanged>(m => m.Context.Message.UserId == _member));

        var afterwards = await As(_member).PostAsJsonAsync(Greetings, new { text = "Hej" });
        Assert.Equal(HttpStatusCode.Forbidden, afterwards.StatusCode);
    }

    private string Enablement => $"/plugins/hello/api/households/{_household}/enablement";

    private string Greetings => $"/plugins/hello/api/households/{_household}/greetings";

    private void GivenHousehold()
    {
        factory.Households.SetRole(_household, _owner, HouseholdRole.Owner);
        factory.Households.SetRole(_household, _member, HouseholdRole.Member);
        factory.Households.SetRole(_household, _guest, HouseholdRole.Guest);
    }

    private async Task GivenSwitchedOn()
    {
        GivenHousehold();
        (await As(_owner).PutAsJsonAsync(Enablement, new EnablementRequest(true))).EnsureSuccessStatusCode();
    }

    private HttpClient As(Guid userId) => factory.CreateAuthenticatedClient(userId);

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : json.ToString();
    }
}

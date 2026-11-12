using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;
using HomeApps.Plugins.Hello;
using HomeApps.Plugins.Hosting;

namespace HomeApps.Plugins.Tests;

/// <summary>
/// jarvis-v1.md: an assistant acting for a person (a token with <c>act</c>) changes plugin data with the person's
/// rights, through the API or the plugin's MCP server, but never switches a plugin on or off.
/// </summary>
public sealed class AssistantTests(HelloPluginFactory factory) : IClassFixture<HelloPluginFactory>
{
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();

    private const string HouseholdName = "Dom";

    private string Enablement => $"/plugins/hello/api/households/{_household}/enablement";

    private string Greetings => $"/plugins/hello/api/households/{_household}/greetings";

    [Fact]
    public async Task An_assistant_writes_plugin_data_with_the_persons_rights()
    {
        await GivenSwitchedOn();

        var forMember = await factory.CreateAssistantClient(_member).PostAsJsonAsync(Greetings, new { text = "Od JARVIS-a" });
        var forGuest = await factory.CreateAssistantClient(_guest).PostAsJsonAsync(Greetings, new { text = "Od JARVIS-a" });

        Assert.Equal(HttpStatusCode.OK, forMember.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forGuest.StatusCode);
        Assert.Equal("plugin.forbidden", await CodeOf(forGuest));
    }

    [Fact]
    public async Task An_assistant_never_switches_a_plugin()
    {
        GivenHousehold();

        var response = await factory.CreateAssistantClient(_owner).PutAsJsonAsync(Enablement, new EnablementRequest(true));
        var state = await factory.CreateAuthenticatedClient(_owner).GetFromJsonAsync<EnablementView>(Enablement);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("assistant.not_allowed", await CodeOf(response));
        Assert.False(state!.Enabled);
    }

    [Fact]
    public async Task The_mcp_server_lists_exactly_the_manifest_tools()
    {
        await using var mcp = await McpTestClient.ConnectAsync(factory.CreateAssistantClient(_member), "plugins/hello/mcp");

        var tools = await mcp.ListToolsAsync();

        Assert.Equal(HelloPlugin.Manifest.Mcp!.Tools.Order(StringComparer.Ordinal), tools.Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.True(tools.Single(t => t.Name == "list_greetings").ProtocolTool.Annotations!.ReadOnlyHint);
    }

    [Fact]
    public async Task Tools_run_the_same_requests_and_answer_in_plain_text()
    {
        await GivenSwitchedOn();
        await using var forMember = await McpTestClient.ConnectAsync(factory.CreateAssistantClient(_member), "plugins/hello/mcp");
        await using var forGuest = await McpTestClient.ConnectAsync(factory.CreateAssistantClient(_guest), "plugins/hello/mcp");

        var written = await forMember.CallTextAsync("say_hello", new Dictionary<string, object?> { ["household"] = "dom", ["text"] = "Hej z MCP" });
        var refused = await forGuest.CallTextAsync("say_hello", new Dictionary<string, object?> { ["household"] = _household.ToString(), ["text"] = "Hej" });
        var listed = await forGuest.CallTextAsync("list_greetings", new Dictionary<string, object?> { ["household"] = "Dom" });
        var unknown = await forGuest.CallTextAsync("list_greetings", new Dictionary<string, object?> { ["household"] = "Działka" });

        Assert.Equal(($"Greeting written in {HouseholdName}.", false), written);
        Assert.True(unknown.IsError);
        Assert.Contains("household.not_found", unknown.Text, StringComparison.Ordinal);
        Assert.True(refused.IsError);
        Assert.Contains("plugin.forbidden", refused.Text, StringComparison.Ordinal);
        Assert.False(listed.IsError);
        Assert.Contains("- Hej z MCP", listed.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_mcp_server_needs_a_token()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/plugins/hello/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Tools_that_differ_from_the_manifest_stop_the_start()
    {
        var missing = Assert.Throws<InvalidOperationException>(() =>
            PluginHostingExtensions.EnsureToolsMatchManifest(HelloPlugin.Manifest, ["list_greetings"]));
        var extra = Assert.Throws<InvalidOperationException>(() =>
            PluginHostingExtensions.EnsureToolsMatchManifest(HelloPlugin.Manifest, ["list_greetings", "say_hello", "wave"]));

        Assert.Contains("not registered: say_hello", missing.Message, StringComparison.Ordinal);
        Assert.Contains("not in the manifest: wave", extra.Message, StringComparison.Ordinal);
        PluginHostingExtensions.EnsureToolsMatchManifest(HelloPlugin.Manifest, ["say_hello", "list_greetings"]);
    }

    private void GivenHousehold()
    {
        factory.Households.HouseholdNames[_household] = HouseholdName;
        factory.Households.SetRole(_household, _owner, HouseholdRole.Owner);
        factory.Households.SetRole(_household, _member, HouseholdRole.Member);
        factory.Households.SetRole(_household, _guest, HouseholdRole.Guest);
    }

    private async Task GivenSwitchedOn()
    {
        GivenHousehold();
        (await factory.CreateAuthenticatedClient(_owner).PutAsJsonAsync(Enablement, new EnablementRequest(true))).EnsureSuccessStatusCode();
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : json.ToString();
    }
}

using System.Net;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Tests;

[Collection(nameof(JarvisCollection))]
public sealed class GatewayTests(JarvisFactory factory)
{
    private const string Endpoint = "jarvis/mcp";
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task Without_a_token_the_answer_points_to_the_resource_metadata_and_from_there_to_Auth()
    {
        var client = factory.CreateClient();

        var refused = await client.PostAsync("/jarvis/mcp", new StringContent("{}"));
        var metadata = JsonDocument.Parse(await client.GetStringAsync("/.well-known/oauth-protected-resource/jarvis/mcp")).RootElement;

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Contains("resource_metadata=", refused.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
        Assert.Contains("/.well-known/oauth-protected-resource/jarvis/mcp", refused.Headers.WwwAuthenticate.ToString(), StringComparison.Ordinal);
        Assert.Equal(JarvisFactory.Resource, metadata.GetProperty("resource").GetString());
        Assert.Equal(TestJwt.Issuer, metadata.GetProperty("authorization_servers")[0].GetString());
    }

    [Fact]
    public async Task A_token_for_another_audience_is_refused()
    {
        var response = await factory.CreateClient(TestJwt.CreateAppToken(_person, "https://elsewhere.test/mcp", "app_x"))
            .PostAsync("/jarvis/mcp", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_connected_app_sees_every_source_prefixed_without_clashes_and_the_sources_see_an_assistant()
    {
        await using var mcp = await Connect(TestJwt.CreateAppToken(_person, JarvisFactory.Resource, "app_claude"));

        var tools = (await mcp.ListToolsAsync()).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var who = await mcp.CallTextAsync("households_my_households", new Dictionary<string, object?>());

        Assert.Equal(["households_my_households", "pomodoro_add_session", "todo_add_task", "todo_delete_task", "todo_my_tasks"], tools);
        Assert.Equal(($"sub={_person} act=jarvis-service", false), who);
        Assert.Contains((_person, "app_claude"), factory.Delegation.Calls);
    }

    [Fact]
    public async Task Calls_go_to_the_source_that_owns_the_name_with_the_arguments()
    {
        await using var mcp = await Connect(TestJwt.CreateToken(_person));

        var todo = await mcp.CallTextAsync("todo_add_task", new Dictionary<string, object?> { ["title"] = "Mleko" });
        var homemix = await mcp.CallTextAsync("pomodoro_add_session", new Dictionary<string, object?> { ["minutes"] = 25 });
        var missing = await mcp.CallTextAsync("accounts_my_profile", new Dictionary<string, object?>());

        Assert.Equal(("Added \"Mleko\".", false), todo);
        Assert.Equal(("Session of 25 min.", false), homemix);
        Assert.True(missing.IsError);
        Assert.Contains("accounts did not answer", missing.Text, StringComparison.Ordinal);
        Assert.Contains((_person, "direct"), factory.Delegation.Calls);
    }

    [Fact]
    public async Task A_delegated_token_from_JARVIS_itself_passes_without_a_new_delegation()
    {
        var person = Guid.NewGuid();
        await using var mcp = await Connect(TestJwt.CreateDelegatedToken(person));

        var who = await mcp.CallTextAsync("households_my_households", new Dictionary<string, object?>());

        Assert.Equal(($"sub={person} act=jarvis-service", false), who);
        Assert.DoesNotContain(factory.Delegation.Calls, c => c.UserId == person);
    }

    [Fact]
    public async Task Resources_pass_through_with_the_source_in_the_uri()
    {
        await using var mcp = await Connect(TestJwt.CreateToken(_person));

        var resource = Assert.Single(await mcp.ListResourcesAsync());
        var read = await mcp.ReadResourceAsync(resource.Uri);

        Assert.Equal("homemix:homemix://pomodoro/summary", resource.Uri);
        var text = Assert.IsType<TextResourceContents>(Assert.Single(read.Contents));
        Assert.Equal(("3 sessions today", "homemix:homemix://pomodoro/summary"), (text.Text, text.Uri));
    }

    private Task<McpClient> Connect(string token) => McpTestClient.ConnectAsync(factory.CreateClient(token), Endpoint);
}

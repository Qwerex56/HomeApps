using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.BuildingBlocks.Testing;

namespace Devices.Api.Tests;

public sealed class LinkTests(DevicesApiFactory api) : IClassFixture<DevicesApiFactory>
{
    [Fact]
    public async Task A_device_says_hello_and_appears_online_with_its_actions()
    {
        var me = Guid.NewGuid();
        await using var device = await FakeDevice.ConnectAsync(api, me, "mac-biuro", FakeDevice.Action("say"), FakeDevice.Action("lights", confirm: true));

        Assert.Equal("ready", device.First.GetProperty("type").GetString());
        using var client = api.CreateAuthenticatedClient(me);
        var mine = Assert.Single((await client.GetFromJsonAsync<JsonElement>("/devices")).EnumerateArray());
        Assert.Equal("mac-biuro", mine.GetProperty("name").GetString());
        Assert.True(mine.GetProperty("online").GetBoolean());
        Assert.Equal(["lights", "say"], mine.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("name").GetString()).Order());
    }

    [Fact]
    public async Task A_bad_hello_is_refused_with_its_code()
    {
        await using var device = await FakeDevice.ConnectAsync(api, Guid.NewGuid(), new string('x', 61));

        Assert.Equal("error", device.First.GetProperty("type").GetString());
        Assert.Equal("devices.invalid_name", device.First.GetProperty("code").GetString());
    }

    [Fact]
    public async Task JARVIS_sees_the_online_actions_as_tools_and_the_device_answers()
    {
        var me = Guid.NewGuid();
        await using var device = await FakeDevice.ConnectAsync(api, me, "mac",
            FakeDevice.Action("say", input: new { type = "object", properties = new { text = new { type = "string" } } }),
            FakeDevice.Action("lights", confirm: true),
            FakeDevice.Action("clipboard", confirm: true, readOnly: true));
        using var http = api.CreateAssistantClient(me);
        await using var mcp = await McpTestClient.ConnectAsync(http);

        var tools = (await mcp.ListToolsAsync()).ToDictionary(t => t.Name);
        Assert.Equal(["mac_clipboard", "mac_lights", "mac_say"], tools.Keys.Order());
        Assert.True(tools["mac_lights"].ProtocolTool.Annotations!.DestructiveHint);
        Assert.False(tools["mac_say"].ProtocolTool.Annotations!.DestructiveHint);
        Assert.True(tools["mac_clipboard"].ProtocolTool.Annotations!.ReadOnlyHint);

        var (text, isError) = await mcp.CallTextAsync("mac_say", new Dictionary<string, object?> { ["text"] = "Cześć" });
        Assert.False(isError);
        Assert.StartsWith("say done:", text, StringComparison.Ordinal);
        Assert.Contains("\"text\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nie_pytaj_turns_the_confirmation_off()
    {
        var me = Guid.NewGuid();
        await using var device = await FakeDevice.ConnectAsync(api, me, "mac", FakeDevice.Action("lights", confirm: true));
        using var client = api.CreateAuthenticatedClient(me);
        var id = device.First.GetProperty("deviceId").GetGuid();

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/devices/{id}/actions/lights/allow", new { allowed = true })).StatusCode);

        using var http = api.CreateAssistantClient(me);
        await using var mcp = await McpTestClient.ConnectAsync(http);
        Assert.False(Assert.Single(await mcp.ListToolsAsync()).ProtocolTool.Annotations!.DestructiveHint);
    }

    [Fact]
    public async Task Another_person_sees_no_tools_and_cannot_call_them()
    {
        await using var device = await FakeDevice.ConnectAsync(api, Guid.NewGuid(), "mac", FakeDevice.Action("say"));
        using var http = api.CreateAssistantClient(Guid.NewGuid());
        await using var mcp = await McpTestClient.ConnectAsync(http);

        Assert.Empty(await mcp.ListToolsAsync());
        var (text, isError) = await mcp.CallTextAsync("mac_say");
        Assert.True(isError);
        Assert.Contains("devices.not_found", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_silent_device_fails_the_call_after_the_timeout()
    {
        var me = Guid.NewGuid();
        await using var device = await FakeDevice.ConnectAsync(api, me, "mac", FakeDevice.Action("say"));
        device.Silent = true;
        using var http = api.CreateAssistantClient(me);
        await using var mcp = await McpTestClient.ConnectAsync(http);

        var (text, isError) = await mcp.CallTextAsync("mac_say");

        Assert.True(isError);
        Assert.Contains("devices.no_answer", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_removed_device_is_cut_off_until_restored()
    {
        var me = Guid.NewGuid();
        Guid id;
        await using (var device = await FakeDevice.ConnectAsync(api, me, "mac", FakeDevice.Action("say")))
        {
            id = device.First.GetProperty("deviceId").GetGuid();
            using var client = api.CreateAuthenticatedClient(me);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/devices/{id}")).StatusCode);

            using var http = api.CreateAssistantClient(me);
            await using var mcp = await McpTestClient.ConnectAsync(http);
            Assert.Empty(await mcp.ListToolsAsync());
        }

        await using (var again = await FakeDevice.ConnectAsync(api, me, "mac"))
        {
            Assert.Equal("devices.disabled", again.First.GetProperty("code").GetString());
        }

        using (var client = api.CreateAuthenticatedClient(me))
        {
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/devices/{id}/restore", null)).StatusCode);
        }

        await using var restored = await FakeDevice.ConnectAsync(api, me, "mac");
        Assert.Equal("ready", restored.First.GetProperty("type").GetString());
        Assert.Equal(id, restored.First.GetProperty("deviceId").GetGuid());
    }

    [Fact]
    public async Task Without_a_token_the_list_is_401()
    {
        using var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/devices")).StatusCode);
    }
}
